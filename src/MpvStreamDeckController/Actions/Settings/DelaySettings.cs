using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the Subtitle Delay and Audio Delay dials.
/// </summary>
/// <param name="StepMilliseconds">Milliseconds to change the delay by per dial tick.</param>
public sealed record DelaySettings(double StepMilliseconds = DelaySettings.DefaultStepMilliseconds)
{
    /// <summary>The same as mpv's own key bindings, which change the delays by 0.1 seconds.</summary>
    public const double DefaultStepMilliseconds = 100;

    private const string StepKey = "delayStep";

    public static DelaySettings Default { get; } = new();

    /// <summary>
    /// Reads the delay settings from an action's settings object. Missing or invalid values fall back to the defaults.
    /// </summary>
    public static DelaySettings Read(JObject? settings)
    {
        return settings is null ? Default : new(StepMilliseconds: SettingsValues.ReadPositiveNumber(settings, StepKey) ?? DefaultStepMilliseconds);
    }

    /// <summary>
    /// The command to change <paramref name="delayProperty"/> (sub-delay or audio-delay) by <paramref name="ticks"/> steps,
    /// earlier if negative. mpv takes the delay in seconds.
    /// </summary>
    public MpvCommand CreateCommand(string delayProperty, int ticks) => new("add", delayProperty, Math.Round(ticks * StepMilliseconds / 1000, 6));
}
