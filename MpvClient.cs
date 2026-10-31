using BarRaider.SdTools;
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
        /// <summary>Opens a duplex connection to mpv. Return null if mpv is unreachable; throw only on cancellation.</summary>
        public delegate Task<Stream?> Connector(TimeSpan timeout, CancellationToken token);

        private readonly Connector _connector;
        public readonly MpvClientOptions _options;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly SemaphoreSlim _connectRequested = new(0, 1);
        private Stream? _mpvStream;

        private bool _isConnected;

        public MpvClient(Connector connector, MpvClientOptions? options = null)
        {
            _connector = connector;
            _options = options ?? new MpvClientOptions();

            _ = Task.Run(() => KeepConnectedAsync(_cancellationTokenSource.Token));
        }

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
                LogHelper.Log(TracingLevel.DEBUG, $"mpv connector failed: {ex.Message}");
                return false;
            }

            if (stream is null)
            {
                return false;
            }

            using (stream)
            {
                _connectRequested.Wait(0); // connected, so any pending launch signal has served its purpose

                try
                {
                    _mpvStream = stream;
                    _isConnected = true;

                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    while (await reader.ReadLineAsync(token) is { } line)
                    {
                        // keep connection open. 
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogHelper.Log(TracingLevel.DEBUG, $"mpv connection ended: {ex.Message}");
                }
                finally
                {
                    _mpvStream = null;

                    if (_isConnected)
                    {
                        _isConnected = false;
                    }
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
                NotifyLaunched(); // mpv may have started without us seeing a launch event: make an idle session look
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
                LogHelper.Log(TracingLevel.WARN, $"Error sending MPV command: {ex.Message}");
                return false;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void NotifyLaunched()
        {
            try { _connectRequested.Release(); }
            catch (SemaphoreFullException) { }
        }
    }
}