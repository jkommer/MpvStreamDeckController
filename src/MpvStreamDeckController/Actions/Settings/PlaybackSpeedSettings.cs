using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the Playback speed dial.
/// </summary>
/// <param name="Step">Playback speed step change per dial tick.</param>
public sealed record PlaybackSpeedSettings(double Step = PlaybackSpeedSettings.DefaultStep)
{
    public const double DefaultStep = 0.1;

    private const string StepKey = "playbackSpeedStep";

    public static PlaybackSpeedSettings Default { get; } = new();

    /// <summary>
    /// Reads the playback speed settings from an action's settings object. Missing or invalid values fall back to the defaults.
    /// </summary>
    public static PlaybackSpeedSettings Read(JObject? settings)
    {
        return settings is null ? Default : new(Step: SettingsValues.ReadPositiveNumber(settings, StepKey) ?? DefaultStep);
    }

    /// <summary>
    /// The command to change the playback speed by <paramref name="ticks"/> steps, down if negative.
    /// </summary>
    public MpvCommand CreateCommand(int ticks) => new("add", "speed", Math.Round(ticks * Step, 3));
}
