using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.audiodelay")]
public class AudioDelayAction(ISDConnection connection, InitialPayload payload) : DelayAction(connection, payload, MpvProperties.AudioDelay);
