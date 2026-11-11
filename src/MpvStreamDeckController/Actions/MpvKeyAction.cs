using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

/// <summary>What a key shows.</summary>
/// <param name="State">Index into the action's <c>States</c> in manifest.json.</param>
/// <param name="Title">Text drawn on the key, for all states.</param>
public readonly record struct KeyFeedback(uint? State = null, string? Title = null);

public abstract class MpvKeyAction : KeypadBase
{
    private readonly MpvStateSubscription<KeyFeedback>? _stateSubscription;
    private volatile CommonActionSettings _settings;
    private volatile MpvState _state = MpvState.Inactive;

    protected MpvKeyAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty>? observedProperties = null)
        : base(connection, payload)
    {
        MpvLaunchHook.EnsureAttached(connection);
        _settings = CommonActionSettings.Read(payload.Settings);

        if (observedProperties is { Count: > 0 })
        {
            _stateSubscription = new(observedProperties, OnStateChanged, OnFeedbackChanged);
        }
    }

    protected abstract MpvCommand[]? Commands { get; }

    /// <summary>
    /// The latest mpv state, for actions whose commands depend on it. It only holds values for the properties this action
    /// observes, and stays <see cref="MpvState.Inactive"/> for an action that observes none.
    /// </summary>
    protected MpvState State => _state;

    /// <summary>What the key should show for a given mpv state. Only called for actions that observe properties.</summary>
    protected virtual KeyFeedback GetFeedback(MpvState state) => default;

    private KeyFeedback OnStateChanged(MpvState state)
    {
        _state = state;
        return GetFeedback(state);
    }

    public override void KeyPressed(KeyPayload payload) => _ = Connection.SendCommandsAsync(_settings.Apply(Commands));
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
        if (feedback.State is null && feedback.Title is null)
        {
            return;
        }

        _ = SetFeedbackAsync(feedback);
    }

    private async Task SetFeedbackAsync(KeyFeedback feedback)
    {
        try
        {
            if (feedback.State is { } state)
            {
                await Connection.SetStateAsync(state);
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
