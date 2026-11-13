using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.togglesubtitlevisibility")]
public class ToggleSubtitleVisibilityAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.SubVisibility])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "sub-visibility")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.SubVisibility) ?? false ? (uint)1 : 0;
        return new(iconState, null);
    }
}
