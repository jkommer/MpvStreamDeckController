using System.Globalization;
using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;

namespace MpvStreamDeckController.Actions;

/// <summary>What a dial shows.</summary>
/// <param name="Text">Value text.</param>
/// <param name="Progress">Indicator bar position, 0-100.</param>
/// <param name="Icon">Path relative to the plugin folder (or a data URI); null leaves the icon alone.</param>
public readonly record struct DialFeedback(string Text = "", double Progress = 0, string? Icon = null);

public abstract class MpvDialAction : KeyAndEncoderBase
{
    private const string BarKey = "indicator";
    private const string ValueKey = "value";
    private const string IconKey = "icon";

    private readonly MpvStateSubscription<DialFeedback>? _stateSubscription;

    protected MpvDialAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty> observedProperties)
        : base(connection, payload)
    {
        MpvLaunchHook.EnsureAttached(connection);

        if (observedProperties is { Count: > 0 })
        {
            _stateSubscription = new MpvStateSubscription<DialFeedback>(observedProperties, GetFeedback, OnFeedbackChanged);
        }
    }

    protected abstract object[] RotateCommand(int ticks);
    protected abstract object[] PressCommand { get; }
    protected abstract DialFeedback GetFeedback(MpvState state);

    public override void DialRotate(DialRotatePayload payload) => _ = MpvClient.Instance.SendAsync(RotateCommand(payload.Ticks));
    public override void DialDown(DialPayload payload) => _ = MpvClient.Instance.SendAsync(PressCommand);
    public override void DialUp(DialPayload payload) { }
    public override void TouchPress(TouchpadPressPayload payload) => _ = MpvClient.Instance.SendAsync(PressCommand);

    public override void KeyPressed(KeyPayload payload) { }
    public override void KeyReleased(KeyPayload payload) { }
    public override void OnTick() { }
    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }
    public override void ReceivedSettings(ReceivedSettingsPayload payload) { }
    private void OnFeedbackChanged(DialFeedback feedback) => _ = SetFeedbackAsync(feedback);

    private async Task SetFeedbackAsync(DialFeedback feedback)
    {
        try
        {
            var values = new Dictionary<string, string>
            {
                [BarKey] = Math.Clamp(feedback.Progress, 0, 100).ToString("0.##", CultureInfo.InvariantCulture),
                [ValueKey] = feedback.Text,
            };

            if (feedback.Icon is not null)
            {
                values[IconKey] = feedback.Icon;
            }

            await Connection.SetFeedbackAsync(values);
        }
        catch (Exception ex)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"SetFeedback failed: {ex.Message}");
        }
    }

    public override void Dispose() => _stateSubscription?.Dispose();
}