using BarRaider.SdTools;
using Newtonsoft.Json;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.customcommand")]
public class CustomCommandAction : MpvKeyAction
{   
    private sealed class CustomCommandSettings
    {
        [JsonProperty("command")]
        public string Command { get; set; } = string.Empty;
    }
    private volatile MpvCommand[]? _commands;

    protected override MpvCommand[]? Commands => _commands;

    public CustomCommandAction(ISDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        ParseCommand(payload.Settings?.ToObject<CustomCommandSettings>());
    }    

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
        => ParseCommand(payload.Settings?.ToObject<CustomCommandSettings>());

    private void ParseCommand(CustomCommandSettings? settings)
    {
        if (MpvCommandParser.TryParse(settings?.Command, out var result, out var error))
        {
            _commands = [.. result.Commands.Select(x => x.ToArray())];
        }
        else
        {
            _commands = null; 

            if (!string.IsNullOrWhiteSpace(settings?.Command))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Custom command is not valid: {settings.Command}, error: {error}");
            }
        }
    }        
}