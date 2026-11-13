using System.Globalization;
using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.framestep")]
public class FrameStepAction(ISDConnection connection, InitialPayload payload)
    : MpvDialAction(connection, payload, [MpvProperties.TimePos, MpvProperties.Duration, MpvProperties.Pause, MpvProperties.ExactTimePos, MpvProperties.ContainerFps])
{
    protected override MpvCommand[] RotateCommands(int ticks) => CreateCommands(ticks);

    protected override MpvCommand[] PressCommands { get; } = [new("cycle", "pause")];

    protected override DialFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    /// <summary>
    /// One frame-step per tick, or one frame-back-step per tick when turned anticlockwise. They are sent as separate
    /// commands rather than as a frame count, which only newer mpv versions accept.
    /// </summary>
    internal static MpvCommand[] CreateCommands(int ticks)
    {
        var command = ticks < 0 ? "frame-back-step" : "frame-step";
        var commands = new MpvCommand[Math.Abs(ticks)];

        for (var i = 0; i < commands.Length; i++)
        {
            commands[i] = new(command);
        }

        return commands;
    }

    /// <summary>
    /// The position and progress bar as on the Seek dial. While paused the text is the frame number instead (e.g. #1234),
    /// if the frame rate is known; it is not shown during playback, where it would change with every frame.
    /// <para>
    /// The frame number is worked out as position times frame rate. mpv has an estimated-frame-number property that does
    /// the same, but mpv never sends change notifications for it, so an observer only ever sees its first value.
    /// </para>
    /// </summary>
    internal static DialFeedback CreateFeedback(MpvState state)
    {
        var position = SeekAction.CreateFeedback(state);

        if (position == default
            || state.Flag(MpvProperties.Pause) != true
            || state.Number(MpvProperties.ExactTimePos) is not { } time
            || state.Number(MpvProperties.ContainerFps) is not { } framesPerSecond
            || framesPerSecond <= 0)
        {
            return position;
        }

        return position with { Text = FormatFrame(time * framesPerSecond) };
    }

    internal static string FormatFrame(double frame) => string.Create(CultureInfo.InvariantCulture, $"#{Math.Max(0, frame):0}");
}
