using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.subtitledelay")]
public class SubtitleDelayAction(ISDConnection connection, InitialPayload payload) : DelayAction(connection, payload, MpvProperties.SubDelay);
