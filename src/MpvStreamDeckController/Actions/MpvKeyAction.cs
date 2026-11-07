using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

/// <summary>What a key shows.</summary>
/// <param name="State">Index into the action's <c>States</c> in manifest.json.</param>
/// <param name="Title">Text drawn on the key, for all states.</param>
public readonly record struct KeyFeedback(uint? State = null, string? Title = null);

public abstract class MpvKeyAction : KeypadBase
{
    private readonly MpvStateSubscription<KeyFeedback>? _stateSubscription;

    protected MpvKeyAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty>? observedProperties = null)
        : base(connection, payload)
    {
        MpvLaunchHook.EnsureAttached(connection); 

        if (observedProperties is { Count: > 0 })
        {
            _stateSubscription = new MpvStateSubscription<KeyFeedback>(observedProperties, GetFeedback, OnFeedbackChanged);
        }
    }

    protected abstract MpvCommand[]? Commands { get; }

    protected virtual KeyFeedback GetFeedback(MpvState state) => default;

    public override void KeyPressed(KeyPayload payload) => _ = Connection.SendCommandsAsync(Commands);
    public override void KeyReleased(KeyPayload payload) { }
    public override void OnTick() { }
    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }
    public override void ReceivedSettings(ReceivedSettingsPayload payload) { }

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
            LogHelper.Log(TracingLevel.ERROR, $"Updating key failed: {ex.Message}");
        }
    }

    public override void Dispose() => _stateSubscription?.Dispose();
}