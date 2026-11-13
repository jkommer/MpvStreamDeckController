using System.Globalization;
using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

/// <summary>
/// A dial for one of mpv's delays (sub-delay or audio-delay): turning it changes the delay by the configured step,
/// pressing it or tapping its touch strip resets the delay to 0, and it shows the delay in milliseconds.
/// </summary>
public abstract class DelayAction(ISDConnection connection, InitialPayload payload, MpvProperty delayProperty)
    : MpvDialAction(connection, payload, [delayProperty])
{
    private volatile DelaySettings _settings = DelaySettings.Read(payload.Settings);

    protected override MpvCommand[] RotateCommands(int ticks) => [_settings.CreateCommand(delayProperty.Name, ticks)];

    protected override MpvCommand[] PressCommands { get; } = [new("set", delayProperty.Name, "0")];

    protected override void OnSettingsChanged(JObject? settings) => _settings = DelaySettings.Read(settings);

    protected override DialFeedback GetFeedback(MpvState state) => CreateFeedback(state, delayProperty);

    /// <summary>What the dial shows for a given mpv state. Separate from the action so it can be tested without a Stream Deck connection.</summary>
    internal static DialFeedback CreateFeedback(MpvState state, MpvProperty delayProperty)
    {
        if (!state.IsConnected || state.Number(delayProperty) is not { } seconds)
        {
            return new("");
        }

        return new(FormatDelay(seconds));
    }

    /// <summary>
    /// Whole milliseconds with a sign unless it is zero, e.g. +150 ms, -50 ms or 0 ms.
    /// </summary>
    internal static string FormatDelay(double seconds)
    {
        var milliseconds = Math.Round(seconds * 1000, MidpointRounding.AwayFromZero);

        // Covers -0 as well, which would otherwise be shown with a sign.
        return milliseconds == 0
            ? "0 ms"
            : string.Create(CultureInfo.InvariantCulture, $"{milliseconds:+0;-0} ms");
    }
}
