using System.Globalization;
using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

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
    private volatile CommonActionSettings _settings;

    protected MpvDialAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty> observedProperties)
        : base(connection, payload)
    {
        MpvLaunchHook.EnsureAttached(connection);
        _settings = CommonActionSettings.Read(payload.Settings);

        if (observedProperties is { Count: > 0 })
        {
            _stateSubscription = new(observedProperties, GetFeedback, OnFeedbackChanged);
        }
    }

    protected abstract MpvCommand[] RotateCommands(int ticks);
    protected abstract MpvCommand[] PressCommands { get; }
    protected abstract DialFeedback GetFeedback(MpvState state);

    public override void DialRotate(DialRotatePayload payload) => SendCommands(RotateCommands(payload.Ticks));
    public override void DialDown(DialPayload payload) => SendCommands(PressCommands);
    public override void DialUp(DialPayload payload) { }

    public override void TouchPress(TouchpadPressPayload payload) => SendCommands(PressCommands);

    public override void KeyPressed(KeyPayload payload) { }
    public override void KeyReleased(KeyPayload payload) { }

    public override void OnTick() { }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }
    public sealed override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        _settings = CommonActionSettings.Read(payload.Settings);
        OnSettingsChanged(payload.Settings);
    }

    /// <summary>Called when the user changes the action's settings; the constructor reads them the first time.</summary>
    protected virtual void OnSettingsChanged(JObject? settings) { }

    private void SendCommands(MpvCommand[]? commands) => _ = Connection.SendCommandsAsync(_settings.Apply(commands));

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
            LogHelper.Log(TracingLevel.ERROR, $"SetFeedback failed: {ex}");
        }
    }

    public override void Dispose() => _stateSubscription?.Dispose();
}
