using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.previousplaylistentry")]
public class PreviousPlaylistEntryAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{        
    protected override MpvCommand[] Commands { get; } = [new("playlist-prev")];
}