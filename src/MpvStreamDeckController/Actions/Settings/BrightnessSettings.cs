using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the Brightness dial.
/// </summary>
/// <param name="Step">How much to change mpv's brightness by per dial tick.</param>
public sealed record BrightnessSettings(double Step = BrightnessSettings.DefaultStep)
{
    /// <summary>The same as mpv's own key bindings, which change the brightness by 1.</summary>
    public const double DefaultStep = 1;

    private const string StepKey = "brightnessStep";

    public static BrightnessSettings Default { get; } = new();

    /// <summary>
    /// Reads the brightness settings from an action's settings object. Missing or invalid values fall back to the defaults.
    /// </summary>
    public static BrightnessSettings Read(JObject? settings)
    {
        return settings is null ? Default : new(Step: SettingsValues.ReadPositiveNumber(settings, StepKey) ?? DefaultStep);
    }

    /// <summary>
    /// The command to change the brightness by <paramref name="ticks"/> steps, darker if negative. mpv clamps the result
    /// to [-100, 100], so no range handling is needed here.
    /// </summary>
    public MpvCommand CreateCommand(int ticks) => new("add", "brightness", Math.Round(ticks * Step, 3));
}
