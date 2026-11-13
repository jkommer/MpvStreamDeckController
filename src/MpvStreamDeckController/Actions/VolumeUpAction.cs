using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.volumeup")]
public class VolumeUpAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    private volatile VolumeSettings _volumeSettings = VolumeSettings.Read(payload.Settings);

    protected override MpvCommand[] Commands => [_volumeSettings.CreateCommand(1)];

    protected override void OnSettingsChanged(JObject? settings) => _volumeSettings = VolumeSettings.Read(settings);
}
