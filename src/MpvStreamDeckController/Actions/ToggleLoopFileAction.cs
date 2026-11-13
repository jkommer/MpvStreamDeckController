using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.toggleloopfile")]
public class ToggleLoopFileAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.LoopFile])
{
    protected override MpvCommand[] Commands { get; } = [new("cycle-values", "loop-file", "inf", "no")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    internal static KeyFeedback CreateFeedback(MpvState state) => LoopFeedback.Create(state, MpvProperties.LoopFile);
}
