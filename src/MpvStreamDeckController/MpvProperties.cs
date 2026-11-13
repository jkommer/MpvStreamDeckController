using System.Text.Json;

namespace MpvStreamDeckController;

/// <summary>
/// An observable mpv property.
/// <para><paramref name="Quantize"/> (numbers only) is applied to every incoming value, so the state only changes
/// when the quantised value does (e.g. whole seconds for time-pos).</para>
/// <para><paramref name="FromJson"/> is for properties whose value is a JSON array or object (e.g. <c>af</c>): it
/// reduces that to the bool, number or string that goes in the state. Without it, such values are ignored.</para>
/// </summary>
public sealed record MpvProperty(long Id, string Name, Func<double, double>? Quantize = null, Func<JsonElement, object?>? FromJson = null);

public static class MpvProperties
{
    private static long _currentId = 1;
    private static readonly Dictionary<long, MpvProperty> _createdProperties = [];

    public static readonly MpvProperty Chapter = Create("chapter");
    public static readonly MpvProperty Chapters = Create("chapters");
    public static readonly MpvProperty TimePos = Create("time-pos", Math.Floor);
    public static readonly MpvProperty Duration = Create("duration", Math.Floor);
    /// <summary>
    /// time-pos again, but not rounded down to whole seconds, for working out the frame number. During playback this
    /// changes with every frame rather than once a second, so every frame produces a new state for its observers; only
    /// observe it where that precision is needed.
    /// </summary>
    public static readonly MpvProperty ExactTimePos = Create("time-pos");
    public static readonly MpvProperty ContainerFps = Create("container-fps");
    public static readonly MpvProperty PlaybackSpeed = Create("speed");
    public static readonly MpvProperty Volume = Create("volume", RoundToTenth);
    public static readonly MpvProperty VolumeMax = Create("volume-max", RoundToTenth);
    public static readonly MpvProperty Mute = Create("mute");
    public static readonly MpvProperty Pause = Create("pause");
    public static readonly MpvProperty OnTop = Create("ontop");
    public static readonly MpvProperty Fullscreen = Create("fullscreen");
    public static readonly MpvProperty SubVisibility = Create("sub-visibility");
    public static readonly MpvProperty LoopFile = Create("loop-file");
    public static readonly MpvProperty LoopPlaylist = Create("loop-playlist");
    public static readonly MpvProperty ACompressor = Create("af", fromJson: FilterEnabled("acompressor"));

    public static IReadOnlyDictionary<long, MpvProperty> All => _createdProperties;

    /// <summary>
    /// One decimal, so a dial stepping by e.g. 0.5 shows every step. Halves round up rather than to the nearest even digit, which is Math.Round's default.
    /// </summary>
    private static double RoundToTenth(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static MpvProperty Create(string name, Func<double, double>? quantize = null, Func<JsonElement, object?>? fromJson = null)
    {
        var propertyId = _currentId++;

        var property = new MpvProperty(propertyId, name, quantize, fromJson);
        _createdProperties.Add(propertyId, property);
        return property;
    }

    /// <summary>
    /// For <c>af</c>, which returns an object (name, label, enabled, params):
    /// true if the filter with this label (or, if it has none, this name) is present and enabled; false otherwise.
    /// </summary>
    private static Func<JsonElement, object?> FilterEnabled(string label) => json =>
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in json.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if ((item.GetPropertyText("label") ?? item.GetPropertyText("name")) != label)
            {
                continue;
            }

            return !item.TryGetProperty("enabled", out var enabled) || enabled.ValueKind != JsonValueKind.False;
        }

        return false;
    };
}
