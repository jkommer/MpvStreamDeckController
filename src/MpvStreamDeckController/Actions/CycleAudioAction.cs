using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.cycleaudio")]
public class CycleAudioAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "audio")];
}
