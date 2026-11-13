using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the Seek dial and the Seek Forward / Seek Back keys.
/// </summary>
/// <param name="Interval">Seconds to seek per dial tick or key press.</param>
/// <param name="Type">Seek precision; null leaves it to mpv's <c>hr-seek</c> option.</param>
public sealed record SeekSettings(double Interval = SeekSettings.DefaultInterval, MpvSeekType? Type = null)
{
    public const double DefaultInterval = 1;

    /// <summary>The default for the keys, which is larger than the dial's: the same as mpv's own arrow keys.</summary>
    public const double DefaultKeyInterval = 5;

    private const string IntervalKey = "seekInterval";
    private const string TypeKey = "seekType";

    public static SeekSettings Default { get; } = new();

    /// <summary>
    /// Reads the seek settings from an action's settings object. Missing or invalid values fall back to the defaults.
    /// </summary>
    /// <param name="settings">The action's settings.</param>
    /// <param name="defaultInterval">The interval to use when none is set: the dial's unless given.</param>
    public static SeekSettings Read(JObject? settings, double defaultInterval = DefaultInterval)
    {
        if (settings is null)
        {
            return defaultInterval == DefaultInterval ? Default : new(defaultInterval);
        }

        return new(
            Interval: SettingsValues.ReadPositiveNumber(settings, IntervalKey) ?? defaultInterval,
            Type: MpvSeekType.TryParse(SettingsValues.ReadString(settings, TypeKey), out var type) ? type : null);
    }

    /// <summary>The command to seek by <paramref name="ticks"/> intervals, backwards if negative; a key press is one interval.</summary>
    public MpvCommand CreateCommand(int ticks)
    {
        // Rounded so e.g. 3 x 0.1 is sent as 0.3, not 0.30000000000000004.
        var seconds = Math.Round(ticks * Interval, 3);

        return new("seek", seconds, Type is null ? "relative" : $"relative+{Type.Flag}");
    }
}
