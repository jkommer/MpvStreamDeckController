using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.seekback")]
public class SeekBackAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    private volatile SeekSettings _seekSettings = SeekSettings.Read(payload.Settings, SeekSettings.DefaultKeyInterval);

    protected override MpvCommand[] Commands => [_seekSettings.CreateCommand(-1)];

    protected override void OnSettingsChanged(JObject? settings) => _seekSettings = SeekSettings.Read(settings, SeekSettings.DefaultKeyInterval);
}
