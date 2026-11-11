using System.Globalization;
using System.Runtime.CompilerServices;
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
    private volatile MpvState _state = MpvState.Inactive;
    private readonly IMpvClient _mpv;

    protected MpvDialAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty> observedProperties)
        : this(connection, payload, MpvClient.Instance, observedProperties)
    {
    }

    /// <summary>For tests: as the other constructor, but talking to <paramref name="mpv"/> instead of the shared <see cref="MpvClient.Instance"/>.</summary>
    private protected MpvDialAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty> observedProperties)
        : base(connection, payload)
    {
        MpvLaunchHook.EnsureAttached(connection);
        _mpv = mpv;
        _settings = CommonActionSettings.Read(payload.Settings);

        if (observedProperties is { Count: > 0 })
        {
            _stateSubscription = new(mpv, observedProperties, OnStateChanged, OnFeedbackChanged);
        }
    }

    /// <summary>The commands to send when the dial is turned by <paramref name="ticks"/>, negative for anticlockwise.</summary>
    protected abstract MpvCommand[] RotateCommands(int ticks);

    /// <summary>The commands to send when the dial is pressed or its touch strip is tapped, read at each press.</summary>
    protected abstract MpvCommand[] PressCommands { get; }

    /// <summary>
    /// The latest mpv state, for actions whose commands depend on it. It only holds values for the properties this action
    /// observes, and stays <see cref="MpvState.Inactive"/> for an action that observes none.
    /// </summary>
    protected MpvState State => _state;

    /// <summary>
    /// What the dial should show for a given mpv state. Only called for actions that observe properties.
    /// <para>
    /// The first call happens inside this class's constructor, before the derived class's constructor body has run. An
    /// override must therefore not use fields that are assigned in a constructor body; field initialisers and primary
    /// constructor parameters are fine. Keeping it a function of <paramref name="state"/> alone avoids the problem.
    /// </para>
    /// </summary>
    protected abstract DialFeedback GetFeedback(MpvState state);

    private DialFeedback OnStateChanged(MpvState state)
    {
        _state = state;
        return GetFeedback(state);
    }

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

    /// <param name="commands">The commands to send.</param>
    /// <param name="trigger">The dial event that triggered the send, for the log; filled in by the compiler.</param>
    private void SendCommands(MpvCommand[] commands, [CallerMemberName] string trigger = "")
        => _ = Connection.SendCommandsAsync(_mpv, _settings.Apply(commands), GetType().Name, trigger);

    private void OnFeedbackChanged(DialFeedback feedback) => _ = SetFeedbackAsync(feedback);

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

    public override void Dispose() => _stateSubscription?.Dispose();
}
