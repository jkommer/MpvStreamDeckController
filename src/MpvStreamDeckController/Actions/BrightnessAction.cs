using System.Globalization;
using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.brightness")]
public class BrightnessAction(ISDConnection connection, InitialPayload payload)
    : MpvDialAction(connection, payload, [MpvProperties.Brightness])
{
    // mpv's brightness runs from -100 to 100, with 0 leaving the picture as it is.
    private const double MinBrightness = -100;
    private const double MaxBrightness = 100;

    private volatile BrightnessSettings _settings = BrightnessSettings.Read(payload.Settings);

    protected override MpvCommand[] RotateCommands(int ticks) => [_settings.CreateCommand(ticks)];

    protected override MpvCommand[] PressCommands { get; } = [new("set", "brightness", "0")];

    protected override void OnSettingsChanged(JObject? settings) => _settings = BrightnessSettings.Read(settings);

    protected override DialFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    /// <summary>What the dial shows for a given mpv state. Separate from the action so it can be tested without a Stream Deck connection.</summary>
    internal static DialFeedback CreateFeedback(MpvState state)
    {
        if (!state.IsConnected || state.Number(MpvProperties.Brightness) is not { } brightness)
        {
            return new("");
        }

        // The bar covers the whole range, so an unchanged picture (0) sits in the middle.
        var progress = (brightness - MinBrightness) / (MaxBrightness - MinBrightness) * 100.0;

        return new(FormatBrightness(brightness), Math.Clamp(progress, 0, 100));
    }

    /// <summary>
    /// With a sign unless it is zero, and a decimal only when there is one, e.g. +5, -2.5 or 0.
    /// </summary>
    internal static string FormatBrightness(double brightness)
    {
        var rounded = Math.Round(brightness, 1, MidpointRounding.AwayFromZero);

        // Covers -0 as well, which would otherwise be shown with a sign.
        return rounded == 0
            ? "0"
            : string.Create(CultureInfo.InvariantCulture, $"{rounded:+0.#;-0.#}");
    }
}
