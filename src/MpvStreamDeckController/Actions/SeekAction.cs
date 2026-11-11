using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.seek")]
public class SeekAction(ISDConnection connection, InitialPayload payload)
    : MpvDialAction(connection, payload, [MpvProperties.TimePos, MpvProperties.Duration])
{
    private volatile SeekSettings _seekSettings = SeekSettings.Read(payload.Settings);

    protected override MpvCommand[] RotateCommands(int ticks) => [_seekSettings.CreateCommand(ticks)];

    protected override MpvCommand[] PressCommands => [new("cycle", "pause")];

    protected override void OnSettingsChanged(JObject? settings)
    {
        _seekSettings = SeekSettings.Read(settings);
    }

    protected override DialFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    /// <summary>What the dial shows for a given mpv state. Separate from the action so it can be tested without a Stream Deck connection.</summary>
    internal static DialFeedback CreateFeedback(MpvState state)
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
