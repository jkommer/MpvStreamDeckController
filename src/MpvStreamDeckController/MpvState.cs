namespace MpvStreamDeckController;

/// <summary>
/// Immutable snapshot: whether mpv is connected, plus the last known value of each observed property. Properties returns null when unavailable.
/// </summary>
public sealed class MpvState
{
    private readonly IReadOnlyDictionary<long, object> _values;

    internal MpvState(long version, bool isConnected, IReadOnlyDictionary<long, object> values)
    {
        Version = version;
        IsConnected = isConnected;
        _values = values;
    }

    public static MpvState Inactive { get; } = new(0, false, new Dictionary<long, object>());

    /// <summary>
    /// Increases with every snapshot. States can be delivered out of order when raised from different threads, so
    /// subscribers use this to ignore one older than what they already applied.
    /// </summary>
    public long Version { get; }

    public bool IsConnected { get; }

    public double? Number(MpvProperty property) => Get(property) is double value ? value : null;

    public bool? Flag(MpvProperty property) => Get(property) is bool value ? value : null;

    public string? Text(MpvProperty property) => Get(property) as string;

    private object? Get(MpvProperty property) => _values.TryGetValue(property.Id, out var value) ? value : null;
}