using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.playpause")]
public class PlayPauseAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override object[] Command { get; } = ["cycle", "pause"];
}
