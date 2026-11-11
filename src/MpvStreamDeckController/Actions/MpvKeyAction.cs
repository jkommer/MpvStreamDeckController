using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

/// <summary>What a key shows. A null member leaves that part of the key as it is, so a key cannot clear a title it has set.</summary>
/// <param name="ImageState">Index into the action's <c>States</c> in manifest.json, which selects the image; null leaves the image alone.</param>
/// <param name="Title">Text drawn on the key, for all states; null leaves the title alone.</param>
public readonly record struct KeyFeedback(uint? ImageState = null, string? Title = null);

/// <summary>An action for a key: pressing it sends <see cref="Commands"/>.</summary>
public abstract class MpvKeyAction : MpvAction<KeyFeedback>
{
    protected MpvKeyAction(ISDConnection connection, InitialPayload payload, IReadOnlyCollection<MpvProperty>? observedProperties = null)
        : base(connection, payload, observedProperties)
    {
    }

    /// <summary>For tests: as the other constructor, but talking to <paramref name="mpv"/> instead of the shared <see cref="MpvClient.Instance"/>.</summary>
    private protected MpvKeyAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty>? observedProperties)
        : base(connection, payload, mpv, observedProperties)
    {
    }

    /// <summary>
    /// The commands to send when the key is pressed, read at each press. Null means the action has nothing valid to send
    /// (e.g. a custom command that doesn't parse); the key then shows the alert.
    /// </summary>
    protected abstract MpvCommand[]? Commands { get; }

    /// <summary>
    /// Most keys show nothing that depends on mpv's state; those that do override this. See the base method for when
    /// it is first called.
    /// </summary>
    protected override KeyFeedback GetFeedback(MpvState state) => default;

    public override void KeyPressed(KeyPayload payload) => SendCommands(() => Commands);

    protected override void ShowFeedback(KeyFeedback feedback)
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
}
