using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.cyclesubtitle")]
public class CycleSubtitleAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override object[] Command { get; } = ["cycle", "sub"];
}
