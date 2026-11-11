using System.Globalization;
using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;

namespace MpvStreamDeckController.Actions;

/// <summary>What a dial shows.</summary>
/// <param name="Text">Value text.</param>
/// <param name="Progress">Indicator bar position, 0-100.</param>
/// <param name="Icon">Path relative to the plugin folder (or a data URI); null leaves the icon alone.</param>
public readonly record struct DialFeedback(string Text = "", double Progress = 0, string? Icon = null);

/// <summary>
/// An action for a dial: turning it sends <see cref="RotateCommands"/>, and pressing it or tapping its touch strip
/// sends <see cref="PressCommands"/>.
/// </summary>
public abstract class MpvDialAction : MpvAction<DialFeedback>
{
    private const string BarKey = "indicator";
    private const string ValueKey = "value";
    private const string IconKey = "icon";

    protected MpvDialAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty> observedProperties)
        : base(connection, payload, observedProperties)
    {
    }

    /// <summary>For tests: as the other constructor, but talking to <paramref name="mpv"/> instead of the shared <see cref="MpvClient.Instance"/>.</summary>
    private protected MpvDialAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty> observedProperties)
        : base(connection, payload, mpv, observedProperties)
    {
    }

    /// <summary>The commands to send when the dial is turned by <paramref name="ticks"/>, negative for anticlockwise.</summary>
    protected abstract MpvCommand[] RotateCommands(int ticks);

    /// <summary>The commands to send when the dial is pressed or its touch strip is tapped, read at each press.</summary>
    protected abstract MpvCommand[] PressCommands { get; }

    public override void DialRotate(DialRotatePayload payload) => SendCommands(RotateCommands(payload.Ticks));
    public override void DialDown(DialPayload payload) => SendCommands(PressCommands);
    public override void TouchPress(TouchpadPressPayload payload) => SendCommands(PressCommands);

    protected override void ShowFeedback(DialFeedback feedback) => _ = SetFeedbackAsync(feedback);

    private async Task SetFeedbackAsync(DialFeedback feedback)
    {
        try
        {
            var values = new Dictionary<string, string>
            {
                [BarKey] = Math.Clamp(feedback.Progress, 0, 100).ToString("0.##", CultureInfo.InvariantCulture),
                // default(DialFeedback) skips the constructor's default, leaving Text null; send that as empty text so the dial is cleared.
                [ValueKey] = feedback.Text ?? "",
            };

            if (feedback.Icon is not null)
            {
                values[IconKey] = feedback.Icon;
            }

            await Connection.SetFeedbackAsync(values);
        }
        catch (Exception ex)
        {
            LogHelper.Log(TracingLevel.ERROR, $"SetFeedback failed: {ex}");
        }
    }
}
