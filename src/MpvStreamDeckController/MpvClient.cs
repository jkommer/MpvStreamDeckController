using BarRaider.SdTools;
using System.IO.Pipes;
using System.Text;

namespace MpvStreamDeckController
{
    public record MpvClientOptions
    {
        public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan ConnectionRetryInterval { get; init; } = TimeSpan.FromMilliseconds(250);
        public TimeSpan LaunchRetryWindow { get; init; } = TimeSpan.FromSeconds(10);
    }

    public class MpvClient
    {
        private const string PipeName = "mpvsocket";

        /// <summary>Opens a duplex connection to mpv. Return null if mpv is unreachable; throw only on cancellation.</summary>
        public delegate Task<Stream?> Connector(TimeSpan timeout, CancellationToken token);

        private readonly Connector _connector;
        private readonly MpvClientOptions _options;
        private readonly MpvStateTracker _stateTracker;

        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly SemaphoreSlim _connectRequested = new(0, 1);
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private Stream? _mpvStream;
        
        /// <summary>
        /// Used to keep track of the stream we registered our observers on. 
        /// </summary>
        private Stream? _observedMpvStream; 

        /// <summary>
        /// Property names that are currently being observed.
        /// </summary>
        private readonly HashSet<string> _registeredObservedProperties = [];

        public MpvClient(Connector connector, MpvClientOptions? options = null)
        {
            _connector = connector;
            _options = options ?? new MpvClientOptions();
            _stateTracker = new MpvStateTracker(() => _ = SyncObservedPropertiesAsync());

            _ = Task.Run(() => KeepConnectedAsync(_cancellationTokenSource.Token));
        }

        public static MpvClient Instance { get; } = new MpvClient(ConnectPipeAsync);

        private static async Task<Stream?> ConnectPipeAsync(TimeSpan timeout, CancellationToken token)
        {
            var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                await pipe.ConnectAsync((int)timeout.TotalMilliseconds, token);
                return pipe;
            }
            catch (Exception)
            {
                pipe.Dispose();
                token.ThrowIfCancellationRequested();
                return null;
            }
        }        

        public event Action<MpvState>? StateChanged
        {
            add => _stateTracker.StateChanged += value;
            remove => _stateTracker.StateChanged -= value;
        }

        public MpvState State => _stateTracker.State;

        private async Task KeepConnectedAsync(CancellationToken token)
        {
            try
            {
                // Try immediately, incase mpv was already running (e.g. we might be (re-)installing this plugin). 
                var retryUntil = DateTime.UtcNow;

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        // A connection that just ended means that instance exited, so sleep. 
                        // An unreachable mpv is retried only while the window opened by a launch event is still open.
                        if (!await ConnectAndListenAsync(token) && DateTime.UtcNow < retryUntil)
                        {
                            await Task.Delay(_options.ConnectionRetryInterval, token);
                            continue;
                        }

                        await _connectRequested.WaitAsync(token);
                        retryUntil = DateTime.UtcNow + _options.LaunchRetryWindow;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        LogHelper.Log(TracingLevel.ERROR, $"mpv session error: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Connects to the mpv pipe and keeps the pipe one by reading any responses we receive. 
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Returns true if a connection was established (and has now ended), false if mpv was unreachable.</returns>
        private async Task<bool> ConnectAndListenAsync(CancellationToken token)
        {
            Stream? stream;
            try
            {
                stream = await _connector(_options.ConnectionTimeout, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"mpv connector failed: {ex.Message}");
                return false;
            }

            if (stream is null)
            {
                return false;
            }

            using (stream)
            {
                // connected, so any pending launch signal has served its purpose
                _connectRequested.Wait(0, token); 

                try
                {
                    _mpvStream = stream;

                    _stateTracker.Connected();
                    _ = SyncObservedPropertiesAsync(); 

                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    while (await reader.ReadLineAsync(token) is { } line)
                    {
                        if (MpvProtocol.TryParsePropertyChange(line, out var id, out var value))
                            _stateTracker.RegisterPropertyChange(id, value);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogHelper.Log(TracingLevel.DEBUG, $"mpv connection ended: {ex.Message}");
                }
                finally
                {
                    _mpvStream = null;
                    _stateTracker.Disconnected();
                }
            }

            return true;
        }

        /// <summary>
        /// Sends a JSON IPC command. 
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Returns false (and logs) if mpv is not connected or the write fails.</returns>
        public async Task<bool> SendAsync(object[] command)
        {
            var pipe = _mpvStream;

            if (pipe is null)
            {
                LogHelper.Log(TracingLevel.WARN, "mpv is not connected; command dropped.");
                NotifyMpvLaunched(); // mpv may have started without us seeing a launch event.
                return false;
            }

            await _writeLock.WaitAsync();

            try
            {
                await pipe.WriteAsync(Encoding.UTF8.GetBytes(MpvProtocol.BuildCommand(command)));
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"Error sending MPV command: {ex.Message}");
                return false;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void NotifyMpvLaunched()
        {
            try { _connectRequested.Release(); }
            catch (SemaphoreFullException) { }
        }

        public IDisposable ObserveProperty(IReadOnlyCollection<MpvProperty> properties) => _stateTracker.ObserveProperty(properties);

        /// <summary>
        /// Sync our observed properties with the connected mpv instance. Adding any missing ones and removing any which
        /// we no longer care about. 
        /// </summary>
        private async Task SyncObservedPropertiesAsync()
        {
            await _writeLock.WaitAsync();

            try
            {
                var pipe = _mpvStream;

                if (pipe is null)
                {
                    return;
                }

                // our connection has changed; get rid of any previous observers as those might no longer apply.
                if (!ReferenceEquals(_observedMpvStream, pipe))
                {
                    _registeredObservedProperties.Clear();
                    _observedMpvStream = pipe;
                }

                var properties = MpvProperties.All;

                foreach (var property in properties.Values)
                {
                    var name = property.Name;
                    var isObserved = _stateTracker.IsPropertyObserved(property);
                    var isRegistered = _registeredObservedProperties.Contains(name);

                    if (isObserved && !isRegistered)
                    {
                        await pipe.WriteAsync(Encoding.UTF8.GetBytes(MpvProtocol.BuildObserveProperty(property.Id, name)));
                        _registeredObservedProperties.Add(name);
                    }
                    else if (!isObserved && isRegistered)
                    {
                        await pipe.WriteAsync(Encoding.UTF8.GetBytes(MpvProtocol.BuildUnobserveProperty(property.Id)));
                        _registeredObservedProperties.Remove(name);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"Syncing mpv observations failed: {ex.Message}");
            }
            finally
            {
                _writeLock.Release();
            }
        }
        
        public void Dispose() => _cancellationTokenSource.Cancel();
    }
}