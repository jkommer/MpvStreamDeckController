using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.playpause")]
public class PlayPauseAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.Pause])
{
    // Indexes into the action's States in manifest.json.
    internal const uint Playing = 0;
    internal const uint Paused = 1;

    protected override MpvCommand[] Commands { get; } = [new("cycle", "pause")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    /// <summary>
    /// The key shows what mpv is doing, not what a press would do: the pause bars while paused, otherwise the play
    /// triangle (also when mpv isn't running or hasn't reported yet).
    /// </summary>
    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.Pause) ?? false ? Paused : Playing;
        return new(iconState, null);
    }
}
