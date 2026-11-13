using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.shuffleplaylist")]
public class ShufflePlaylistAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override MpvCommand[] Commands { get; } = [new("playlist-shuffle")];
}
