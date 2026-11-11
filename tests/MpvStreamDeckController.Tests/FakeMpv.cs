using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Shouldly;

namespace MpvStreamDeckController.Tests;

/// <summary>
/// The server end of the mpv named pipe, with an <see cref="MpvClient"/> that connects to it.
/// The server can be closed and started again on the same pipe name to simulate mpv exiting and relaunching.
/// </summary>
internal sealed class FakeMpv : IAsyncDisposable
{
    private readonly string _pipeName = $"mpv-test-{Guid.NewGuid():N}";
    private NamedPipeServerStream _server;
    private StreamReader _reader;
    private int _connectCalls;
    private volatile bool _failWrites;
    private volatile bool _serverOpen;
    private TaskCompletionSource? _writeGate;
    private volatile TaskCompletionSource _writeHeld = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MpvClient Client { get; }

    /// <summary>How many times the client has called its connector, whether or not it connected.</summary>
    public int ConnectCalls => Volatile.Read(ref _connectCalls);

    /// <summary>When true, the client's writes to the pipe throw.</summary>
    public bool FailWrites
    {
        get => _failWrites;
        set => _failWrites = value;
    }

    /// <summary>
    /// Makes the client's next writes wait until <see cref="ReleaseWrites"/>, or until the client gives up on them. A held
    /// write keeps the client's write lock, so anything else that needs to write queues up behind it.
    /// </summary>
    public void HoldWrites()
    {
        _writeHeld = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref _writeGate, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    /// <summary>Waits until a write is being held by <see cref="HoldWrites"/>.</summary>
    public Task WaitUntilAWriteIsHeldAsync() => _writeHeld.Task.WithTimeout();

    /// <summary>Lets writes held by <see cref="HoldWrites"/> continue, and stops holding new ones.</summary>
    public void ReleaseWrites() => Interlocked.Exchange(ref _writeGate, null)?.TrySetResult();

    private FakeMpv(MpvClientOptions? options)
    {
        (_server, _reader) = CreateServer();
        Client = new MpvClient(ConnectAsync, options);
    }

    /// <summary>Starts the server and a client, and waits until the client is connected.</summary>
    public static async Task<FakeMpv> StartAsync(MpvClientOptions? options = null)
    {
        var mpv = new FakeMpv(options);
        await mpv.WaitForConnectionAsync();
        return mpv;
    }

    /// <summary>Closes the server end, as if mpv exited.</summary>
    public void CloseServer()
    {
        _serverOpen = false;
        _reader.Dispose();
        _server.Dispose();
    }

    /// <summary>Starts a new server on the same pipe name, as if mpv relaunched. Call <see cref="CloseServer"/> first.</summary>
    public void StartServer() => (_server, _reader) = CreateServer();

    /// <summary>Waits for the client to connect to the current server and report itself connected.</summary>
    public async Task WaitForConnectionAsync()
    {
        await _server.WaitForConnectionAsync(TaskTestExtensions.CancellationToken).WithTimeout();
        await WaitForStateAsync(state => state.IsConnected);
    }

    /// <summary>Returns the client's state once it matches <paramref name="predicate"/>.</summary>
    public async Task<MpvState> WaitForStateAsync(Func<MpvState, bool> predicate)
    {
        var matched = new TaskCompletionSource<MpvState>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnStateChanged(MpvState state)
        {
            if (predicate(state))
            {
                matched.TrySetResult(state);
            }
        }

        // Subscribe before checking the current state, so a change in between isn't missed.
        Client.StateChanged += OnStateChanged;

        try
        {
            var current = Client.State;
            return predicate(current) ? current : await matched.Task.WithTimeout();
        }
        finally
        {
            Client.StateChanged -= OnStateChanged;
        }
    }

    /// <summary>Reads the next command, which must carry a request_id. <c>Command</c> is the raw JSON array.</summary>
    public async Task<(long Id, string Command)> ReadRequestAsync()
    {
        var (requestId, command, line) = await ReadAsync();
        requestId.ShouldNotBeNull($"Expected a command with a request_id, but received: {line}");
        return (requestId.Value, command);
    }

    /// <summary>Reads the next command, which must not carry a request_id (observe_property etc.). Returns the raw JSON array.</summary>
    public async Task<string> ReadUnnumberedCommandAsync()
    {
        var (requestId, command, line) = await ReadAsync();
        requestId.ShouldBeNull($"Expected a command without a request_id, but received: {line}");
        return command;
    }

    /// <summary>Reads (and discards) everything until the client closes its end of the pipe.</summary>
    public Task WaitForClientToCloseAsync() => _reader.ReadToEndAsync(TaskTestExtensions.CancellationToken).WithTimeout();

    public Task ReplyAsync(long requestId, string error)
        => WriteLineAsync(JsonSerializer.Serialize(new { request_id = requestId, error, data = (object?)null }));

    public Task SendPropertyChangeAsync(MpvProperty property, object? value)
        => WriteLineAsync(JsonSerializer.Serialize(new { @event = "property-change", id = property.Id, name = property.Name, data = value }));

    public async Task WriteLineAsync(string line)
    {
        await _server.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"), TaskTestExtensions.CancellationToken);
        await _server.FlushAsync(TaskTestExtensions.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        _reader.Dispose();
        await _server.DisposeAsync();
    }

    private (NamedPipeServerStream, StreamReader) CreateServer()
    {
        var server = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        _serverOpen = true;
        return (server, new StreamReader(server, Encoding.UTF8));
    }

    private async Task<(long? RequestId, string Command, string Line)> ReadAsync()
    {
        var line = await _reader.ReadLineAsync(TaskTestExtensions.CancellationToken).WithTimeout();
        line.ShouldNotBeNull("The client closed the pipe.");

        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        long? requestId = root.TryGetProperty("request_id", out var id) ? id.GetInt64() : null;
        return (requestId, root.GetProperty("command").GetRawText(), line);
    }

    /// <summary>Behaves like the real connector: null when mpv is unreachable, throws only on cancellation.</summary>
    private async Task<Stream?> ConnectAsync(TimeSpan timeout, CancellationToken token)
    {
        Interlocked.Increment(ref _connectCalls);

        // Like the real connector's pipe check: no server means null straight away. A flag rather than MpvClient.PipeExists,
        // because a just-closed pipe can still be listed until the client's handle closes.
        if (!_serverOpen)
        {
            return null;
        }

        var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            await pipe.ConnectAsync((int)timeout.TotalMilliseconds, token);
            return new InterceptingStream(pipe, BeforeWriteAsync);
        }
        catch (Exception)
        {
            await pipe.DisposeAsync();
            token.ThrowIfCancellationRequested();
            return null;
        }
    }

    private async Task BeforeWriteAsync(CancellationToken token)
    {
        if (_failWrites)
        {
            throw new IOException("Simulated write failure.");
        }

        if (Volatile.Read(ref _writeGate) is { } gate)
        {
            _writeHeld.TrySetResult();
            await gate.Task.WaitAsync(token);
        }
    }

    /// <summary>Passes everything through to the pipe, but runs <c>beforeWrite</c> (which may delay or throw) before each write.</summary>
    private sealed class InterceptingStream(Stream inner, Func<CancellationToken, Task> beforeWrite) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);

        public override void Write(byte[] buffer, int offset, int count)
        {
            beforeWrite(CancellationToken.None).GetAwaiter().GetResult();
            inner.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await beforeWrite(cancellationToken);
            await inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await beforeWrite(cancellationToken);
            await inner.WriteAsync(buffer, cancellationToken);
        }

        public override void Flush() => inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
