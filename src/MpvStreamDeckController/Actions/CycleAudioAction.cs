using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.cycleaudio")]
public class CycleAudioAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override object[][] Commands { get; } = [["cycle", "audio"]];
}
