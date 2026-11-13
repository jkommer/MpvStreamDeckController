using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.abloop")]
public class AbLoopAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.AbLoopA, MpvProperties.AbLoopB])
{
    // Indexes into the action's States in manifest.json.
    internal const uint Off = 0;
    internal const uint StartSet = 1;
    internal const uint Looping = 2;

    /// <summary>mpv's ab-loop command steps through the same three states: set the start, set the end, clear both.</summary>
    protected override MpvCommand[] Commands { get; } = [new("ab-loop")];

    protected override KeyFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    /// <summary>
    /// Off with no loop point, the start image with one, and the looping image with both. mpv reports a point as a
    /// number of seconds when it is set and as "no" when it is not, so only a number counts as set.
    /// </summary>
    internal static KeyFeedback CreateFeedback(MpvState state)
    {
        var points = (state.Number(MpvProperties.AbLoopA) is null ? 0 : 1) + (state.Number(MpvProperties.AbLoopB) is null ? 0 : 1);

        return new(points switch { 2 => Looping, 1 => StartSet, _ => Off }, null);
    }
}
