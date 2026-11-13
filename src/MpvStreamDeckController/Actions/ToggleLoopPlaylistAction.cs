using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.toggleloopplaylist")]
public class ToggleLoopPlaylistAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.LoopPlaylist])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle-values", "loop-playlist", "inf", "no")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state) => LoopFeedback.Create(state, MpvProperties.LoopPlaylist);
}
