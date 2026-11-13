using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.quit")]
public class QuitAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    private volatile QuitSettings _quitSettings = QuitSettings.Read(payload.Settings);

    protected override MpvCommand[] Commands => [_quitSettings.CreateCommand()];

    protected override void OnSettingsChanged(JObject? settings) => _quitSettings = QuitSettings.Read(settings);
}
