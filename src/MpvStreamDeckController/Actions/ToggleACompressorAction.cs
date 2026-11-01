using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions
{
    [PluginActionId("com.jkommer.mpv.toggleacompressor")]
    public class ToggleACompressorAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.ACompressor])
    {
        protected override object[] Command { get; } = ["script-message", "toggle-acompressor"];

        protected override KeyFeedback GetFeedback(MpvState state)
        {
            var iconState = state.Flag(MpvProperties.ACompressor) ?? false ? (uint) 1 : 0;
            return new (iconState, null);
        }
    }
}
