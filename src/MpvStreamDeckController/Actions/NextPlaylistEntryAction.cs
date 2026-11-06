using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.nextplaylistentry")]
public class NextPlaylistEntryAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{        
    protected override object[][] Commands { get; } = [["playlist-next"]];
}