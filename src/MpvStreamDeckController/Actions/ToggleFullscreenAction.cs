using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.togglefullscreen")]
public class ToggleFullscreenAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.Fullscreen])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "fullscreen")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.Fullscreen) ?? false ? (uint)1 : 0;
        return new(iconState, null);
    }
}
