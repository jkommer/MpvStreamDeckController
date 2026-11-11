using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

/// <summary>What a key shows. A null member leaves that part of the key as it is, so a key cannot clear a title it has set.</summary>
/// <param name="ImageState">Index into the action's <c>States</c> in manifest.json, which selects the image; null leaves the image alone.</param>
/// <param name="Title">Text drawn on the key, for all states; null leaves the title alone.</param>
public readonly record struct KeyFeedback(uint? ImageState = null, string? Title = null);

public abstract class MpvKeyAction : KeypadBase
{
    private readonly MpvStateSubscription<KeyFeedback>? _stateSubscription;
    private volatile CommonActionSettings _settings;
    private volatile MpvState _state = MpvState.Inactive;
    private readonly IMpvClient _mpv;

    protected MpvKeyAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty>? observedProperties = null)
        : this(connection, payload, MpvClient.Instance, observedProperties)
    {
    }

    /// <summary>For tests: as the other constructor, but talking to <paramref name="mpv"/> instead of the shared <see cref="MpvClient.Instance"/>.</summary>
    private protected MpvKeyAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty>? observedProperties)
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

    /// <summary>
    /// The commands to send when the key is pressed, read at each press. Null means the action has nothing valid to send
    /// (e.g. a custom command that doesn't parse); the key then shows the alert.
    /// </summary>
    protected abstract MpvCommand[]? Commands { get; }

    /// <summary>
    /// The latest mpv state, for actions whose commands depend on it. It only holds values for the properties this action
    /// observes, and stays <see cref="MpvState.Inactive"/> for an action that observes none.
    /// </summary>
    protected MpvState State => _state;

    /// <summary>
    /// What the key should show for a given mpv state. Only called for actions that observe properties.
    /// <para>
    /// The first call happens inside this class's constructor, before the derived class's constructor body has run. An
    /// override must therefore not use fields that are assigned in a constructor body; field initialisers and primary
    /// constructor parameters are fine. Keeping it a function of <paramref name="state"/> alone avoids the problem.
    /// </para>
    /// </summary>
    protected virtual KeyFeedback GetFeedback(MpvState state) => default;

    private KeyFeedback OnStateChanged(MpvState state)
    {
        _state = state;
        return GetFeedback(state);
    }

    public override void KeyPressed(KeyPayload payload) => _ = Connection.SendCommandsAsync(_mpv, _settings.Apply(Commands), GetType().Name);
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

    private void OnFeedbackChanged(KeyFeedback feedback)
    {
        if (feedback.ImageState is null && feedback.Title is null)
        {
            return;
        }

        _ = SetFeedbackAsync(feedback);
    }

    private async Task SetFeedbackAsync(KeyFeedback feedback)
    {
        try
        {
            if (feedback.ImageState is { } imageState)
            {
                await Connection.SetStateAsync(imageState);
            }

            if (feedback.Title is not null)
            {
                await Connection.SetTitleAsync(feedback.Title);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log(TracingLevel.ERROR, $"Updating key failed: {ex}");
        }
    }

    public override void Dispose() => _stateSubscription?.Dispose();
}
