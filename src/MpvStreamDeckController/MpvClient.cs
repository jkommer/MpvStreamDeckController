using BarRaider.SdTools;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;

namespace MpvStreamDeckController
{

    public sealed class MpvClient : IMpvStateSource, IDisposable
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
        private readonly Task _connectionTask;
        private int _disposed;

        /// <summary>
        /// The live connection, or null. Written by the connection loop and read by senders on other threads.
        /// </summary>
        private volatile Stream? _mpvStream;
        
        /// <summary>
        /// Used to keep track of the stream we registered our observers on. 
        /// </summary>
        private Stream? _observedMpvStream; 

        /// <summary>
        /// Properties registered with mpv: property id -> the tracker's observation generation at the time.
        /// </summary>
        private readonly Dictionary<long, int> _registeredObservedProperties = [];

        /// <summary>
        /// Commands waiting for mpv's reply, by request_id. IDs start at 1 so they never match the replies to commands sent without one (observe_property etc.), which mpv gives request_id 0.
        /// </summary>
        private readonly ConcurrentDictionary<long, TaskCompletionSource<MpvSendResult>> _pendingReplies = new();
        private long _lastRequestId;

        /// <summary>
        /// Completed when the next connect attempt finishes, whether or not it connected, then replaced for the one after.
        /// Lets a send made while disconnected wait for a reconnect without guessing how long it takes.
        /// </summary>
        private TaskCompletionSource _connectAttemptFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MpvClient(Connector connector, MpvClientOptions? options = null)
        {
            _connector = connector;
            _options = options ?? new MpvClientOptions();
            _stateTracker = new MpvStateTracker(() => _ = SyncObservedPropertiesAsync());

            _connectionTask = Task.Run(() => KeepConnectedAsync(_cancellationTokenSource.Token));
        }

        private static readonly Lazy<MpvClient> LazyInstance = new(() => new MpvClient(ConnectPipeAsync));

        public static MpvClient Instance => LazyInstance.Value;

        /// <summary>
        /// Disposes <see cref="Instance"/> if it was ever created. Call once when the plugin is shutting down.
        /// </summary>
        public static void Shutdown()
        {
            if (LazyInstance.IsValueCreated)
            {
                LazyInstance.Value.Dispose();
            }
        }

        private static async Task<Stream?> ConnectPipeAsync(TimeSpan timeout, CancellationToken token)
        {
            // Connecting to a pipe that doesn't exist waits out the whole timeout, so check first.
            if (PipeExists(PipeName) == false)
            {
                return null;
            }

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

        /// <summary>
        /// Whether a local named pipe exists. Returns null if we were unable to determine the pipe's existence.
        /// </summary>
        internal static bool? PipeExists(string name)
        {
            try
            {
                return Directory.EnumerateFiles(@"\\.\pipe\", name).Any();
            }
            catch (Exception)
            {
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
                            // A send waiting for a connection cuts the pause short rather than waiting it out.
                            await _connectRequested.WaitAsync(_options.ConnectionRetryInterval, token);
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
                SignalConnectAttemptFinished();
                return false;
            }

            if (stream is null)
            {
                SignalConnectAttemptFinished();
                return false;
            }

            using (stream)
            {
                // connected, so any pending launch signal has served its purpose
                _connectRequested.Wait(0, token); 

                try
                {
                    _mpvStream = stream;
                    // must be after setting _mpvStream, so waiting senders find it
                    SignalConnectAttemptFinished(); 

                    _stateTracker.Connected();
                    _ = SyncObservedPropertiesAsync(); 

                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    while (await reader.ReadLineAsync(token) is { } line)
                    {
                        switch (MpvProtocol.ParseMessage(line))
                        {
                            case MpvPropertyChange change:
                                _stateTracker.RegisterPropertyChange(change.PropertyId, change.Value);
                                break;

                            case MpvReply reply when _pendingReplies.TryRemove(reply.RequestId, out var pending):
                                pending.TrySetResult(reply.IsSuccess ? MpvSendResult.Success : MpvSendResult.Rejected(reply.Error));
                                break;
                        }
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

                    // No more replies can arrive on this connection.
                    foreach (var requestId in _pendingReplies.Keys)
                    {
                        if (_pendingReplies.TryRemove(requestId, out var pending))
                        {
                            pending.TrySetResult(MpvSendResult.NoReply);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Sends commands one at a time, waiting for each reply, and stops at the first failure so later commands don't run after an earlier one was rejected.
        /// </summary>
        /// <returns>The first failure, or else the last command's result (<see cref="MpvSendResult.Success"/> if there were none).</returns>
        public async Task<MpvSendResult> SendAsync(IEnumerable<MpvCommand> commands)
        {
            var result = MpvSendResult.Success;

            foreach (var command in commands)
            {
                result = await SendAsync(command);

                if (result.IsFailure)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Sends a command and waits for mpv's reply.
        /// </summary>
        public async Task<MpvSendResult> SendAsync(MpvCommand command)
        {
            var pipe = _mpvStream ?? await WaitForConnectionAsync();

            if (pipe is null)
            {
                LogHelper.Log(TracingLevel.WARN, "mpv is not running or not reachable; command dropped.");
                return MpvSendResult.NotConnected;
            }

            // Register before writing so a fast reply can't arrive before anyone is waiting for it.
            var requestId = Interlocked.Increment(ref _lastRequestId);
            var reply = new TaskCompletionSource<MpvSendResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            _pendingReplies[requestId] = reply;

            try
            {
                var json = MpvProtocol.BuildCommand(command, requestId);

                await _writeLock.WaitAsync();

                try
                {
                    await WriteAsync(pipe, json);
                }
                catch (Exception ex)
                {
                    LogHelper.Log(TracingLevel.ERROR, $"Error sending mpv command {json.TrimEnd()}: {ex.Message}");
                    return MpvSendResult.WriteFailed;
                }
                finally
                {
                    _writeLock.Release();
                }

                MpvSendResult result;

                try
                {
                    result = await reply.Task.WaitAsync(_options.ReplyTimeout);
                }
                catch (TimeoutException)
                {
                    result = MpvSendResult.NoReply;
                }

                switch (result.Status)
                {
                    case MpvSendStatus.Rejected:
                        LogHelper.Log(TracingLevel.WARN, $"mpv rejected {json.TrimEnd()}: {result.Error}");
                        break;

                    case MpvSendStatus.NoReply:
                        LogHelper.Log(TracingLevel.DEBUG, $"No reply from mpv for {json.TrimEnd()}");
                        break;
                }

                return result;
            }
            finally
            {
                _pendingReplies.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// Writes one message to mpv, giving up after <see cref="MpvClientOptions.WriteTimeout"/>. Call with <see cref="_writeLock"/> held.
        /// A write only stalls when mpv has stopped reading the pipe (e.g. it is frozen); without a limit it would hold the write lock
        /// and every later send would queue up behind it forever.
        /// </summary>
        private async Task WriteAsync(Stream pipe, string json)
        {
            using var timeout = new CancellationTokenSource(_options.WriteTimeout);

            try
            {
                await pipe.WriteAsync(Encoding.UTF8.GetBytes(json), timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                // Part of the message may have been written, so the connection can't be used any more. Closing it ends the
                // read loop, which reports the disconnect; the next send or launch event then reconnects.
                pipe.Dispose();
                throw new TimeoutException($"mpv did not accept the write within {_options.WriteTimeout.TotalMilliseconds:0} ms; connection closed.");
            }
        }

        public void NotifyMpvLaunched()
        {
            try { _connectRequested.Release(); }
            catch (SemaphoreFullException) { }
        }

        /// <summary>
        /// Asks the connection loop to connect (mpv may have started without us seeing a launch event, or after the
        /// retry window closed) and waits for that attempt to finish. Returns the stream, or null if mpv is unreachable.
        /// </summary>
        private async Task<Stream?> WaitForConnectionAsync()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return null;
            }

            var attemptFinished = Volatile.Read(ref _connectAttemptFinished);

            // A connection made just before reading the signal has already replaced it, so check again.
            if (_mpvStream is { } connected)
            {
                return connected;
            }

            NotifyMpvLaunched();

            try
            {
                // The attempt normally finishes in milliseconds; the limit only matters if connecting stalls.
                await attemptFinished.Task.WaitAsync(_options.ConnectionTimeout);
            }
            catch (TimeoutException)
            {
            }

            return _mpvStream;
        }

        /// <summary>
        /// Called when an attempt to connect to mpv finishes, whether it connected or not. Wakes every send waiting in <see cref="WaitForConnectionAsync"/>, 
        /// and puts a fresh signal in place for the next connect attempt to complete.
        /// </summary>
        private void SignalConnectAttemptFinished()
        {
            var finished = Interlocked.Exchange(ref _connectAttemptFinished, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            finished.TrySetResult();
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

                foreach (var property in MpvProperties.All.Values)
                {
                    var generation = _stateTracker.GetObservationGeneration(property);
                    var isRegistered = _registeredObservedProperties.TryGetValue(property.Id, out var registeredGeneration);

                    if (isRegistered && generation != registeredGeneration)
                    {
                        // No longer observed, or dropped and re-added since it was registered. In the latter case its
                        // value was discarded, and mpv won't resend it until it changes; registering again makes mpv
                        // send the current value straight away.
                        await WriteAsync(pipe, MpvProtocol.BuildUnobserveProperty(property.Id));
                        _registeredObservedProperties.Remove(property.Id);
                        isRegistered = false;
                    }

                    if (generation is { } current && !isRegistered)
                    {
                        await WriteAsync(pipe, MpvProtocol.BuildObserveProperty(property.Id, property.Name));
                        _registeredObservedProperties[property.Id] = current;
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
        
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _cancellationTokenSource.Cancel();

            // Release any sends waiting for a connection; there won't be one.
            SignalConnectAttemptFinished();

            try
            {
                if (!_connectionTask.Wait(_options.ShutdownTimeout))
                {
                    LogHelper.Log(TracingLevel.WARN, "mpv connection loop did not stop in time.");
                    return;
                }
            }
            catch (AggregateException ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"mpv connection loop failed: {ex.InnerException?.Message}");
            }

            // The semaphores are deliberately not disposed: senders may still be waiting on them, and SemaphoreSlim
            // only holds an unmanaged handle if AvailableWaitHandle is used, which it is not.
            _cancellationTokenSource.Dispose();
        }
    }
}