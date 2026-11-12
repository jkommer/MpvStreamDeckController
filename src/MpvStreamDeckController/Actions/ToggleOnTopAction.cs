using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.toggleontop")]
public class ToggleOnTopAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.OnTop])
{
    protected override MpvCommand[]? Commands => [new("cycle", "ontop")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.OnTop) ?? false ? (uint)1 : 0;
        return new(iconState, null);
    }
}
