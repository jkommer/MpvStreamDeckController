using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvClientTests
{
    #region Disposal

    [Fact]
    public async Task Dispose_WhileConnected_ClosesTheConnectionAndStopsReconnecting()
    {
        // A long connection timeout, so a send that wrongly waited for a reconnect is clearly slower than one that didn't.
        await using var mpv = await FakeMpv.StartAsync(new() { ConnectionTimeout = TimeSpan.FromSeconds(5) });

        mpv.Client.Dispose();

        mpv.Client.State.IsConnected.ShouldBeFalse();
        await mpv.WaitForClientToCloseAsync();

        // Both of these would normally trigger a reconnect. The send must not wait for one either.
        mpv.Client.NotifyMpvLaunched();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        (await mpv.Client.SendAsync(new MpvCommand("cycle", "pause"))).Status.ShouldBe(MpvSendStatus.NotConnected);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2), "should return straight away, not wait for the 5 s timeout");

        // A reconnect would happen on the background loop, so give it time to show up before checking it didn't.
        await Task.Delay(TimeSpan.FromMilliseconds(250), TaskTestExtensions.CancellationToken);
        mpv.ConnectCalls.ShouldBe(1);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var client = new MpvClient((_, _) => Task.FromResult<Stream?>(null));

        client.Dispose();
        Should.NotThrow(client.Dispose);
    }

    [Fact]
    public async Task Dispose_WhileASendIsWaitingForItsReply_ReturnsNoReplyWithoutWaitingForTheTimeout()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { ReplyTimeout = TimeSpan.FromMinutes(5) });

        var send = mpv.Client.SendAsync(new MpvCommand("cycle", "pause"));
        await mpv.ReadRequestAsync();
        mpv.Client.Dispose();

        (await send.WithTimeout()).ShouldBe(MpvSendResult.NoReply);
    }

    #endregion

    #region Connecting and reconnecting

    [Fact]
    public async Task NotifyMpvLaunched_AfterMpvRestarted_Reconnects()
    {
        await using var mpv = await FakeMpv.StartAsync();

        mpv.CloseServer();
        await mpv.WaitForStateAsync(state => !state.IsConnected);

        mpv.StartServer();
        mpv.Client.NotifyMpvLaunched();
        await mpv.WaitForConnectionAsync();

        mpv.ConnectCalls.ShouldBe(2);
    }

    [Fact]
    public async Task SendAsync_WhenMpvRestartedButTheClientHasNotReconnected_ConnectsAndSends()
    {
        await using var mpv = await FakeMpv.StartAsync();

        mpv.CloseServer();
        await mpv.WaitForStateAsync(state => !state.IsConnected);
        mpv.StartServer(); // no launch notification, e.g. mpv started under a different program name

        var send = mpv.Client.SendAsync(new MpvCommand("cycle", "pause"));
        await mpv.WaitForConnectionAsync();
        var (requestId, _) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(requestId, "success");

        (await send.WithTimeout()).ShouldBe(MpvSendResult.Success);
    }

    [Fact]
    public async Task SendAsync_SeveralWhileNotConnected_AllSendOnceConnected()
    {
        await using var mpv = await FakeMpv.StartAsync();

        mpv.CloseServer();
        await mpv.WaitForStateAsync(state => !state.IsConnected);
        mpv.StartServer();

        var sends = Enumerable.Range(0, 3)
            .Select(i => Task.Run(() => mpv.Client.SendAsync(new MpvCommand("add", "volume", i)), TaskTestExtensions.CancellationToken))
            .ToArray();

        await mpv.WaitForConnectionAsync();

        for (var i = 0; i < sends.Length; i++)
        {
            var (requestId, _) = await mpv.ReadRequestAsync();
            await mpv.ReplyAsync(requestId, "success");
        }

        (await Task.WhenAll(sends).WithTimeout()).ShouldAllBe(result => result == MpvSendResult.Success);
        mpv.ConnectCalls.ShouldBe(2);
    }

    [Fact]
    public async Task SendAsync_WhenMpvIsNotRunning_ReturnsNotConnectedWithoutWaitingForTheConnectTimeout()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { ConnectionTimeout = TimeSpan.FromSeconds(5) });

        mpv.CloseServer();
        await mpv.WaitForStateAsync(state => !state.IsConnected);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await mpv.Client.SendAsync(new MpvCommand("cycle", "pause")).WithTimeout();

        result.ShouldBe(MpvSendResult.NotConnected);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2), "should return once the connect attempt fails, not wait for the 5 s timeout");
    }

    [Fact]
    public void PipeExists_ReflectsWhetherTheServerIsListening()
    {
        var name = $"mpv-test-{Guid.NewGuid():N}";

        MpvClient
            .PipeExists(name)
            .ShouldBe(false);

        using (new System.IO.Pipes.NamedPipeServerStream(name))
        {
            MpvClient
                .PipeExists(name)
                .ShouldBe(true);
        }

        MpvClient
            .PipeExists(name)
            .ShouldBe(false);
    }

    #endregion

    #region Observed properties

    [Fact]
    public async Task ObserveProperty_WhileConnected_RegistersWithMpvAndTracksChangesUntilReleased()
    {
        await using var mpv = await FakeMpv.StartAsync();
        var pause = MpvProperties.Pause;

        var handle = mpv.Client.ObserveProperty([pause]);
        (await mpv.ReadUnnumberedCommandAsync()).ShouldBe($"[\"observe_property\",{pause.Id},\"pause\"]");

        await mpv.SendPropertyChangeAsync(pause, true);
        await mpv.WaitForStateAsync(state => state.Flag(pause) == true);

        await mpv.SendPropertyChangeAsync(pause, false);
        await mpv.WaitForStateAsync(state => state.Flag(pause) == false);

        handle.Dispose();
        (await mpv.ReadUnnumberedCommandAsync()).ShouldBe($"[\"unobserve_property\",{pause.Id}]");
    }

    [Fact]
    public async Task ObserveProperty_DroppedAndReAddedBeforeTheClientCouldUnregisterIt_RegistersAgainSoMpvResendsTheValue()
    {
        await using var mpv = await FakeMpv.StartAsync();
        var pause = MpvProperties.Pause;

        var first = mpv.Client.ObserveProperty([pause]);
        await mpv.ReadUnnumberedCommandAsync();
        await mpv.SendPropertyChangeAsync(pause, true);
        await mpv.WaitForStateAsync(state => state.Flag(pause) == true);

        // Keep the write lock busy, so the sync triggered by dropping the property can't run before it is re-added.
        mpv.HoldWrites();
        var send = mpv.Client.SendAsync(new MpvCommand("cycle", "mute"));
        await mpv.WaitUntilAWriteIsHeldAsync();

        first.Dispose();
        mpv.Client.State.Flag(pause).ShouldBeNull();
        using var second = mpv.Client.ObserveProperty([pause]);

        mpv.ReleaseWrites();
        var (requestId, _) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(requestId, "success");
        await send.WithTimeout();

        // mpv kept observing throughout, so it would never resend the unchanged value without this.
        (await mpv.ReadUnnumberedCommandAsync()).ShouldBe($"[\"unobserve_property\",{pause.Id}]");
        (await mpv.ReadUnnumberedCommandAsync()).ShouldBe($"[\"observe_property\",{pause.Id},\"pause\"]");

        await mpv.SendPropertyChangeAsync(pause, true);
        await mpv.WaitForStateAsync(state => state.Flag(pause) == true);
    }

    [Fact]
    public async Task ObserveProperty_AfterReconnecting_RegistersAgainAndStartsFromAnEmptyState()
    {
        await using var mpv = await FakeMpv.StartAsync();
        var volume = MpvProperties.Volume;

        using var handle = mpv.Client.ObserveProperty([volume]);
        await mpv.ReadUnnumberedCommandAsync();
        await mpv.SendPropertyChangeAsync(volume, 40.0);
        await mpv.WaitForStateAsync(state => state.Number(volume) == 40);

        mpv.CloseServer();
        (await mpv.WaitForStateAsync(state => !state.IsConnected)).Number(volume).ShouldBeNull();

        mpv.StartServer();
        mpv.Client.NotifyMpvLaunched();
        await mpv.WaitForConnectionAsync();

        (await mpv.ReadUnnumberedCommandAsync()).ShouldBe($"[\"observe_property\",{volume.Id},\"volume\"]");
    }

    #endregion

    #region Sending one command

    [Fact]
    public async Task SendAsync_WhenMpvRepliesSuccess_ReturnsSuccess()
    {
        await using var mpv = await FakeMpv.StartAsync();

        var send = mpv.Client.SendAsync(new MpvCommand("seek", 1, "relative"));
        var (requestId, command) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(requestId, "success");

        (await send.WithTimeout()).ShouldBe(MpvSendResult.Success);

        // Numbers must stay numbers: mpv treats ["seek", "1"] differently from ["seek", 1].
        command.ShouldBe("[\"seek\",1,\"relative\"]");
    }

    [Fact]
    public async Task SendAsync_WhenMpvRepliesWithAnError_ReturnsRejectedWithTheError()
    {
        await using var mpv = await FakeMpv.StartAsync();

        var send = mpv.Client.SendAsync(new MpvCommand("seek", 1, "relative"));
        var (requestId, _) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(requestId, "error running command");

        var result = await send.WithTimeout();
        result.Status.ShouldBe(MpvSendStatus.Rejected);
        result.Error.ShouldBe("error running command");
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_RepliesOutOfOrderWithEventsAndUnnumberedRepliesBetween_AreMatchedByRequestId()
    {
        await using var mpv = await FakeMpv.StartAsync();

        var first = mpv.Client.SendAsync(new MpvCommand("playlist-next"));
        var (firstRequestId, _) = await mpv.ReadRequestAsync();
        var second = mpv.Client.SendAsync(new MpvCommand("playlist-prev"));
        var (secondRequestId, _) = await mpv.ReadRequestAsync();

        firstRequestId.ShouldNotBe(secondRequestId);
        firstRequestId.ShouldBeGreaterThan(0);

        await mpv.WriteLineAsync("{\"event\":\"seek\"}");
        await mpv.ReplyAsync(0, "invalid parameter"); // e.g. the reply to an observe_property, which has no request_id
        await mpv.ReplyAsync(secondRequestId, "error running command");
        await mpv.SendPropertyChangeAsync(MpvProperties.Pause, true);
        await mpv.ReplyAsync(firstRequestId, "success");

        (await first.WithTimeout()).ShouldBe(MpvSendResult.Success);
        (await second.WithTimeout()).ShouldBe(MpvSendResult.Rejected("error running command"));
    }

    [Fact]
    public async Task SendAsync_WhenAWriteStalls_FailsAfterTheWriteTimeoutAndClosesTheConnection()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { WriteTimeout = TimeSpan.FromMilliseconds(100) });
        mpv.HoldWrites();

        // The second send queues behind the first one's write lock; it must not be left waiting forever.
        var stalled = mpv.Client.SendAsync(new MpvCommand("cycle", "pause"));
        await mpv.WaitUntilAWriteIsHeldAsync();
        var queued = mpv.Client.SendAsync(new MpvCommand("cycle", "mute"));

        (await stalled.WithTimeout()).ShouldBe(MpvSendResult.WriteFailed);
        (await queued.WithTimeout()).ShouldBe(MpvSendResult.WriteFailed);

        await mpv.WaitForStateAsync(state => !state.IsConnected);
        await mpv.WaitForClientToCloseAsync();
    }

    [Fact]
    public async Task SendAsync_WhenMpvDoesNotReply_ReturnsNoReplyAfterTheTimeout()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { ReplyTimeout = TimeSpan.FromMilliseconds(200) });

        var send = mpv.Client.SendAsync(new MpvCommand("subprocess", "sleep"));
        await mpv.ReadRequestAsync();

        var result = await send.WithTimeout();
        result.ShouldBe(MpvSendResult.NoReply);
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenAReplyArrivesAfterTheTimeout_IgnoresItAndLaterCommandsStillWork()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { ReplyTimeout = TimeSpan.FromMilliseconds(100) });

        var lateSendTask = mpv.Client.SendAsync(new MpvCommand("cycle", "pause"));
        var (lateRequestId, _) = await mpv.ReadRequestAsync();
        (await lateSendTask.WithTimeout()).ShouldBe(MpvSendResult.NoReply);
        await mpv.ReplyAsync(lateRequestId, "error running command");

        var next = mpv.Client.SendAsync(new MpvCommand("cycle", "mute"));
        var (nextRequestId, _) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(nextRequestId, "success");

        (await next.WithTimeout()).ShouldBe(MpvSendResult.Success);
    }

    [Fact]
    public async Task SendAsync_WhenTheConnectionClosesBeforeTheReply_ReturnsNoReplyWithoutWaitingForTheTimeout()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { ReplyTimeout = TimeSpan.FromMinutes(5) });

        var send = mpv.Client.SendAsync(new MpvCommand("quit"));
        await mpv.ReadRequestAsync();
        mpv.CloseServer();

        (await send.WithTimeout()).ShouldBe(MpvSendResult.NoReply);
    }

    [Fact]
    public async Task SendAsync_WhenTheWriteFails_ReturnsWriteFailed()
    {
        await using var mpv = await FakeMpv.StartAsync();
        mpv.FailWrites = true;

        var result = await mpv.Client
            .SendAsync(new MpvCommand("cycle", "pause"))
            .WithTimeout();

        result.ShouldBe(MpvSendResult.WriteFailed);
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_ManyAtOnceWithRepliesInRandomOrder_EachGetsItsOwnReply()
    {
        const int count = 50;
        await using var mpv = await FakeMpv.StartAsync();

        var sends = Enumerable.Range(0, count)
            .Select(i => Task.Run(() => mpv.Client.SendAsync(new MpvCommand("show-text", $"message {i}")), TaskTestExtensions.CancellationToken))
            .ToArray();

        var requests = new (long Id, string Command)[count];

        for (var i = 0; i < count; i++)
        {
            requests[i] = await mpv.ReadRequestAsync();
        }

        requests
            .Select(r => r.Id)
            .Distinct()
            .Count()
            .ShouldBe(count);

        // Reply to each with its own command as the error text, so every result shows which reply it was given.
        new Random(42).Shuffle(requests);

        foreach (var (requestId, requestCommand) in requests)
        {
            await mpv.ReplyAsync(requestId, requestCommand);
        }

        var results = await Task
            .WhenAll(sends)
            .WithTimeout();

        for (var i = 0; i < count; i++)
        {
            results[i].ShouldBe(MpvSendResult.Rejected($"[\"show-text\",\"message {i}\"]"));
        }
    }

    #endregion

    #region Sending several commands

    [Fact]
    public async Task SendAsync_Batch_WhenACommandIsRejected_StopsAndDoesNotSendTheRest()
    {
        await using var mpv = await FakeMpv.StartAsync();

        var sendTask = mpv.Client.SendAsync([new("set", "pause", "yes"), new("bogus-command"), new("set", "mute", "yes")]);

        var (firstCommandId, _) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(firstCommandId, "success");

        var (secondCommandId, secondCommand) = await mpv.ReadRequestAsync();
        secondCommand.ShouldBe("[\"bogus-command\"]");

        await mpv.ReplyAsync(secondCommandId, "invalid parameter");

        (await sendTask.WithTimeout()).ShouldBe(MpvSendResult.Rejected("invalid parameter"));

        // The next thing mpv receives is a new command, not the third one from the batch.
        var next = mpv.Client.SendAsync(new MpvCommand("cycle", "pause"));
        var (nextRequestId, nextRequestCommand) = await mpv.ReadRequestAsync();

        nextRequestCommand.ShouldBe("[\"cycle\",\"pause\"]");

        await mpv.ReplyAsync(nextRequestId, "success");
        await next.WithTimeout();
    }

    [Fact]
    public async Task SendAsync_Batch_WhenACommandGetsNoReply_CarriesOnWithTheRest()
    {
        await using var mpv = await FakeMpv.StartAsync(new() { ReplyTimeout = TimeSpan.FromMilliseconds(100) });

        var sendTask = mpv.Client.SendAsync([new("subprocess", "sleep"), new("set", "mute", "yes")]);

        await mpv.ReadRequestAsync(); // no reply
        var (secondCommandId, secondCommand) = await mpv.ReadRequestAsync();
        secondCommand.ShouldBe("[\"set\",\"mute\",\"yes\"]");
        await mpv.ReplyAsync(secondCommandId, "success");

        (await sendTask.WithTimeout()).ShouldBe(MpvSendResult.Success);
    }

    [Fact]
    public async Task SendAsync_Batch_WhenAllSucceed_SendsEachInOrderAndReturnsSuccess()
    {
        await using var mpv = await FakeMpv.StartAsync();

        var sendTask = mpv.Client.SendAsync([new("set", "pause", "yes"), new("set", "mute", "yes")]);

        var (firstCommandId, firstCommand) = await mpv.ReadRequestAsync();
        firstCommand.ShouldBe("[\"set\",\"pause\",\"yes\"]");
        await mpv.ReplyAsync(firstCommandId, "success");

        var (secondCommandId, secondCommand) = await mpv.ReadRequestAsync();
        secondCommand.ShouldBe("[\"set\",\"mute\",\"yes\"]");
        await mpv.ReplyAsync(secondCommandId, "success");

        (await sendTask.WithTimeout()).ShouldBe(MpvSendResult.Success);
    }

    [Fact]
    public async Task SendAsync_Batch_WhenAnotherBatchIsInProgress_WaitsForItInsteadOfInterleaving()
    {
        await using var mpv = await FakeMpv.StartAsync();

        var firstBatchTask = mpv.Client.SendAsync([new("set", "pause", "yes"), new("seek", 0, "absolute")]);
        var secondBatchTask = mpv.Client.SendAsync([new("set", "mute", "yes"), new("add", "volume", 5)]);

        string[] expected = ["[\"set\",\"pause\",\"yes\"]", "[\"seek\",0,\"absolute\"]", "[\"set\",\"mute\",\"yes\"]", "[\"add\",\"volume\",5]"];

        foreach (var command in expected)
        {
            var (requestId, requestCommand) = await mpv.ReadRequestAsync();
            requestCommand.ShouldBe(command);
            await mpv.ReplyAsync(requestId, "success");
        }

        (await firstBatchTask.WithTimeout()).ShouldBe(MpvSendResult.Success);
        (await secondBatchTask.WithTimeout()).ShouldBe(MpvSendResult.Success);
    }

    [Fact]
    public async Task SendAsync_Batch_WhenInProgress_DoesNotHoldBackASingleCommand()
    {
        await using var mpv = await FakeMpv.StartAsync();

        // Send a batch of commands - but do not reply yet. 
        var batchSendTask = mpv.Client.SendAsync([new("set", "pause", "yes"), new("seek", 0, "absolute")]);
        var (batchId, _) = await mpv.ReadRequestAsync();

        // The batch is still waiting for its first reply; a single command should go straight through.
        var singleSendTask = mpv.Client.SendAsync([new("add", "volume", 5)]);
        var (singleRequestId, singleRequestCommand) = await mpv.ReadRequestAsync();

        singleRequestCommand.ShouldBe("[\"add\",\"volume\",5]");
        await mpv.ReplyAsync(singleRequestId, "success");
        (await singleSendTask.WithTimeout()).ShouldBe(MpvSendResult.Success);

        await mpv.ReplyAsync(batchId, "success");
        var (secondBatchId, _) = await mpv.ReadRequestAsync();
        await mpv.ReplyAsync(secondBatchId, "success");
        (await batchSendTask.WithTimeout()).ShouldBe(MpvSendResult.Success);
    }

    [Fact]
    public async Task SendAsync_Batch_WithNoCommands_ReturnsSuccess()
    {
        using var client = new MpvClient((_, _) => Task.FromResult<Stream?>(null));
        (await client.SendAsync([])).ShouldBe(MpvSendResult.Success);
    }

    #endregion
}
