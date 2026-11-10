namespace MpvStreamDeckController;

/// <summary>Where an action gets mpv's state from.</summary>
public interface IMpvStateSource
{
    MpvState State { get; }

    event Action<MpvState>? StateChanged;

    /// <summary>Observes the properties until the returned handle is disposed.</summary>
    IDisposable ObserveProperty(IReadOnlyCollection<MpvProperty> properties);
}
