using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions
{
    [PluginActionId("com.jkommer.mpv.nextplaylistentry")]
    public class NextPlaylistEntryAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
    {        
        protected override object[] Command { get; } = ["playlist-next"];
    }
}