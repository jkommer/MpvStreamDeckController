using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.seek")]
public class SeekAction(ISDConnection connection, InitialPayload payload) : MpvDialAction(connection, payload, [MpvProperties.TimePos, MpvProperties.Duration])
{
    protected override object[] RotateCommand(int ticks) => ["seek", ticks, "relative"];

    protected override object[] PressCommand => ["cycle", "pause"];

    protected override DialFeedback GetFeedback(MpvState state)
    {
        if (!state.IsConnected || state.Number(MpvProperties.TimePos) is not { } time)
        {
            return default;
        }
        
        var position = Math.Max(0, time);
        var duration = Math.Max(0, state.Number(MpvProperties.Duration) ?? 0);

        var progress = duration > 0 ? Math.Clamp(position / duration * 100.0, 0, 100) : 0;
        var useHours = position >= 3600 || duration >= 3600;

        return new(FormatClock(position, useHours), progress);
    }

    private static string FormatClock(double seconds, bool useHours)
        => useHours
            ? TimeSpan.FromSeconds(seconds).ToString(@"h\:mm\:ss")
            : TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
}