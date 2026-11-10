using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the Volume dial.
/// </summary>
/// <param name="Step">Volume percentage points to change per dial tick.</param>
public sealed record VolumeSettings(double Step = VolumeSettings.DefaultStep)
{
    public const double DefaultStep = 2;

    private const string StepKey = "volumeStep";

    public static VolumeSettings Default { get; } = new();

    /// <summary>
    /// Reads the volume settings from an action's settings object. Missing or invalid values fall back to the defaults.
    /// </summary>
    public static VolumeSettings Read(JObject? settings)
    {
        return settings is null ? Default : new(Step: SettingsValues.ReadPositiveNumber(settings, StepKey) ?? DefaultStep);
    }

    /// <summary>
    /// The command to change the volume by <paramref name="ticks"/> steps, down if negative. mpv clamps the result to
    /// [0, volume-max], so no range handling is needed here.
    /// </summary>
    public MpvCommand CreateCommand(int ticks) => new("add", "volume", Math.Round(ticks * Step, 3));
}
