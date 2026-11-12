using System.Globalization;
using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.playbackspeed")]
public class PlaybackSpeedAction(ISDConnection connection, InitialPayload payload)
    : MpvDialAction(connection, payload, [MpvProperties.PlaybackSpeed])
{
    private volatile PlaybackSpeedSettings _settings = PlaybackSpeedSettings.Read(payload.Settings);

    protected override MpvCommand[] RotateCommands(int ticks) => [_settings.CreateCommand(ticks)];

    protected override MpvCommand[] PressCommands { get; } = [new("set", "speed", "1.0")];

    protected override void OnSettingsChanged(JObject? settings) => _settings = PlaybackSpeedSettings.Read(settings);

    protected override DialFeedback GetFeedback(MpvState state) => CreateFeedback(state);

    /// <summary>What the dial shows for a given mpv state. Separate from the action so it can be tested without a Stream Deck connection.</summary>
    internal static DialFeedback CreateFeedback(MpvState state)
    {
        if (!state.IsConnected || state.Number(MpvProperties.PlaybackSpeed) is not { } playbackSpeed)
        {
            return new("");
        }

        return new(FormatPlaybackSpeed(playbackSpeed));
    }

    /// <summary>
    /// Always use two decimals, e.g. 1.00x.
    /// </summary>
    internal static string FormatPlaybackSpeed(double playbackSpeed) => string.Create(CultureInfo.InvariantCulture, $"{playbackSpeed:0.00}x");
}
