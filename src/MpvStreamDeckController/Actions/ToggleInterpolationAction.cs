using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.toggleinterpolation")]
public class ToggleInterpolationAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.Interpolation])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle", "interpolation")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var iconState = state.Flag(MpvProperties.Interpolation) ?? false ? (uint)1 : 0;
        return new(iconState, null);
    }
}
