namespace MpvStreamDeckController.Tests;

/// <summary>
/// An <see cref="IMpvClient"/> for action tests: records what is sent and observed, and lets the test raise state changes.
/// Sends complete immediately, so an action's whole reaction to an event has happened by the time the event method returns.
/// </summary>
internal sealed class FakeMpvClient : IMpvClient
{
    /// <summary>Every batch of commands sent, in order.</summary>
    public List<MpvCommand[]> Sent { get; } = [];

    /// <summary>The properties currently observed, once per observer.</summary>
    public List<MpvProperty> ObservedProperties { get; } = [];

    /// <summary>What <see cref="SendAsync"/> returns.</summary>
    public MpvSendResult SendResult { get; set; } = MpvSendResult.Success;

    public MpvState State { get; private set; } = MpvState.Inactive;

    public event Action<MpvState>? StateChanged;

    public Task<MpvSendResult> SendAsync(IEnumerable<MpvCommand> commands)
    {
        Sent.Add([.. commands]);
        return Task.FromResult(SendResult);
    }

    public IDisposable ObserveProperty(IReadOnlyCollection<MpvProperty> properties)
    {
        ObservedProperties.AddRange(properties);
        return new Release(() =>
        {
            foreach (var property in properties)
            {
                ObservedProperties.Remove(property);
            }
        });
    }

    /// <summary>Makes <paramref name="state"/> the current state and tells subscribers about it.</summary>
    public void Raise(MpvState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }

    private sealed class Release(Action release) : IDisposable
    {
        public void Dispose() => release();
    }
}
