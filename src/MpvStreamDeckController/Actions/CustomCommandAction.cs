using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.customcommand")]
public class CustomCommandAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    private volatile MpvCommand[]? _commands = CustomCommandSettings.Read(payload.Settings).CreateCommands();

    protected override MpvCommand[]? Commands => _commands;

    protected override void OnSettingsChanged(JObject? settings)
    {
        _commands = CustomCommandSettings.Read(settings).CreateCommands();
    }
}
