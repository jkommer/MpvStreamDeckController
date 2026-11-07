using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.playpause")]
public class PlayPauseAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "pause")];
}
