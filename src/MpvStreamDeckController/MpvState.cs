namespace MpvStreamDeckController;

/// <summary>
/// Immutable snapshot: whether mpv is connected, plus the last known value of each observed property. Properties returns null when unavailable.
/// </summary>
public sealed class MpvState
{
    private readonly IReadOnlyDictionary<int, object> _values;

    internal MpvState(bool isConnected, IReadOnlyDictionary<int, object> values)
    {
        IsConnected = isConnected;
        _values = values;
    }

    public static MpvState Inactive { get; } = new(false, new Dictionary<int, object>());

    public bool IsConnected { get; }

    public double? Number(MpvProperty property) => Get(property) is double value ? value : null;

    public bool? Flag(MpvProperty property) => Get(property) is bool value ? value : null;

    public string? Text(MpvProperty property) => Get(property) as string;

    private object? Get(MpvProperty property) => _values.TryGetValue(property.Id, out var value) ? value : null;
}