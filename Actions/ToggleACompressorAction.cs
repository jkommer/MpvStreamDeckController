using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions
{
    [PluginActionId("com.jkommer.mpv.toggleacompressor")]
    public class ToggleACompressorAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
    {
        protected override object[] Command { get; } = ["script-message", "toggle-acompressor"];
    }
}
