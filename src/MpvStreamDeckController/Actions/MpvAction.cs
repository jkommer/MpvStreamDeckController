using System.Runtime.CompilerServices;
using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

/// <summary>
/// What every mpv action shares, whether it sits on a key or a dial: the settings common to all actions, sending
/// commands to mpv, and following mpv's state to keep what the action shows up to date.
/// <para>
/// It handles both key and dial events, and ignores all of them unless a derived class overrides the handler. Which of
/// the two an action can actually be placed on is decided by its <c>Controllers</c> entry in manifest.json, not here.
/// </para>
/// </summary>
/// <typeparam name="TFeedback">What the action shows, e.g. <see cref="KeyFeedback"/> or <see cref="DialFeedback"/>.</typeparam>
public abstract class MpvAction<TFeedback> : KeyAndEncoderBase where TFeedback : struct, IEquatable<TFeedback>
{
    private readonly MpvStateSubscription<TFeedback>? _stateSubscription;
    private readonly IMpvClient _mpv;
    private volatile CommonActionSettings _settings;
    private volatile MpvState _state = MpvState.Inactive;

    /// <param name="connection">The action's connection to Stream Deck.</param>
    /// <param name="payload">The payload the action was created with, holding its saved settings.</param>
    /// <param name="observedProperties">The mpv properties the action needs, for its feedback or its commands; null or empty for none.</param>
    protected MpvAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty>? observedProperties)
        : this(connection, payload, MpvClient.Instance, observedProperties)
    {
    }

    /// <summary>For tests: as the other constructor, but talking to <paramref name="mpv"/> instead of the shared <see cref="MpvClient.Instance"/>.</summary>
    private protected MpvAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty>? observedProperties)
        : base(connection, payload)
    {
        MpvLaunchHook.EnsureAttached(connection);
        _mpv = mpv;
        _settings = CommonActionSettings.Read(payload.Settings);

        if (observedProperties is { Count: > 0 })
        {
            _stateSubscription = new(mpv, observedProperties, OnStateChanged, ShowFeedback);
        }
    }

    /// <summary>
    /// The latest mpv state, for actions whose commands depend on it. It only holds values for the properties this action
    /// observes, and stays <see cref="MpvState.Inactive"/> for an action that observes none.
    /// </summary>
    protected MpvState State => _state;

    /// <summary>
    /// What the action should show for a given mpv state. Only called for actions that observe properties.
    /// <para>
    /// The first call happens inside this class's constructor, before the derived class's constructor body has run. An
    /// override must therefore not use fields that are assigned in a constructor body; field initialisers and primary
    /// constructor parameters are fine. Keeping it a function of <paramref name="state"/> alone avoids the problem.
    /// </para>
    /// </summary>
    protected abstract TFeedback GetFeedback(MpvState state);

    /// <summary>
    /// Puts <paramref name="feedback"/> on the key or dial. Called whenever <see cref="GetFeedback"/> returns something
    /// different from last time, the first time from this class's constructor (see <see cref="GetFeedback"/>). It is
    /// called while a lock is held, so it must only start the update, not wait for it.
    /// </summary>
    protected abstract void ShowFeedback(TFeedback feedback);

    /// <summary>Called when the user changes the action's settings; the constructor reads them the first time.</summary>
    protected virtual void OnSettingsChanged(JObject? settings) { }

    /// <summary>
    /// Sends the commands to mpv with the action's common settings applied, and shows the alert if that fails or there
    /// are no commands. Returns straight away; the send carries on in the background.
    /// <para>
    /// The commands are passed as a function so that working them out is covered too: if it throws, that is logged and
    /// the alert is shown, instead of the exception reaching the Stream Deck library, which would end the plugin.
    /// </para>
    /// </summary>
    /// <param name="getCommands">Returns the commands to send, or null if the action has nothing valid to send.</param>
    /// <param name="trigger">The event that triggered the send, for the log; filled in by the compiler.</param>
    protected void SendCommands(Func<MpvCommand[]?> getCommands, [CallerMemberName] string trigger = "")
        => _ = Connection.SendCommandsAsync(_mpv, () => _settings.Apply(getCommands()), GetType().Name, trigger);

    private TFeedback OnStateChanged(MpvState state)
    {
        _state = state;
        return GetFeedback(state);
    }

    public sealed override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        _settings = CommonActionSettings.Read(payload.Settings);
        OnSettingsChanged(payload.Settings);
    }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }
    public override void OnTick() { }

    public override void KeyPressed(KeyPayload payload) { }
    public override void KeyReleased(KeyPayload payload) { }

    public override void DialRotate(DialRotatePayload payload) { }
    public override void DialDown(DialPayload payload) { }
    public override void DialUp(DialPayload payload) { }
    public override void TouchPress(TouchpadPressPayload payload) { }

    public override void Dispose() => _stateSubscription?.Dispose();
}
