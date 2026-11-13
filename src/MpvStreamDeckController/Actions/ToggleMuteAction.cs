using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.togglemute")]
public class ToggleMuteAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.Mute])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "mute")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.Mute) ?? false ? (uint)1 : 0;
        return new(iconState, null);
    }
}
