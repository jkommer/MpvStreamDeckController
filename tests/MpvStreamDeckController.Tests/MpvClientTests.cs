using System.IO.Pipes;
using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Dispose_WhileConnected_ClosesTheConnectionAndStopsReconnecting()
    {
        var pipeName = $"mpv-test-{Guid.NewGuid():N}";
        await using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        var connectCalls = 0;
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Stream?> Connect(TimeSpan timeout, CancellationToken token)
        {
            Interlocked.Increment(ref connectCalls);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync((int)timeout.TotalMilliseconds, token);
            return client;
        }

        var waitForClient = server.WaitForConnectionAsync(TestContext.Current.CancellationToken);
        var client = new MpvClient(Connect);
        client.StateChanged += state => { if (state.IsConnected) connected.TrySetResult(); };

        // The connection may have completed before the handler was attached.
        if (client.State.IsConnected)
        {
            connected.TrySetResult();
        }

        await waitForClient.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await connected.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        client.Dispose();

        client.State.IsConnected.ShouldBeFalse();

        // The client end was closed, so the server reads end-of-stream.
        var buffer = new byte[1024];
        while (await server.ReadAsync(buffer, TestContext.Current.CancellationToken).AsTask().WaitAsync(Timeout, TestContext.Current.CancellationToken) > 0)
        {
            // drain anything the client sent (observe_property commands) until the pipe closes
        }

        client.NotifyMpvLaunched();
        (await client.SendAsync(new MpvCommand("cycle", "pause"))).ShouldBeFalse();
        connectCalls.ShouldBe(1);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var client = new MpvClient((_, _) => Task.FromResult<Stream?>(null));

        client.Dispose();
        Should.NotThrow(client.Dispose);
    }
}
