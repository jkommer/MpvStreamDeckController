using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.toggledeband")]
public class ToggleDebandAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.Deband])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "deband")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.Deband) ?? false ? (uint)1 : 0;
        return new(iconState, null);
    }
}
