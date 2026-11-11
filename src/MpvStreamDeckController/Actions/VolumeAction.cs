using System.Globalization;
using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.volume")]
public class VolumeAction(ISDConnection connection, InitialPayload payload) : MpvDialAction(connection, payload, [MpvProperties.Volume, MpvProperties.VolumeMax, MpvProperties.Mute])
{
    private const double DefaultVolumeMax = 100;

    // Paths relative to the plugin folder; must match the files you ship.
    private const string VolumeIcon = "Images/dial/11a_volume_dial";
    private const string MutedIcon = "Images/dial/11b_volume_muted_dial";

    private volatile VolumeSettings _volumeSettings = VolumeSettings.Read(payload.Settings);

    protected override MpvCommand[] RotateCommands(int ticks) => [_volumeSettings.CreateCommand(ticks)];

    protected override MpvCommand[] PressCommands => [new("cycle", "mute")];

    protected override void OnSettingsChanged(JObject? settings)
    {
        _volumeSettings = VolumeSettings.Read(settings);
    }

    /// <summary>
    /// Whole volumes as e.g. 42%, fractional ones with one decimal, e.g. 42.5%.
    /// </summary>
    internal static string FormatVolume(double volume) => string.Create(CultureInfo.InvariantCulture, $"{volume:0.#}%");

    protected override DialFeedback GetFeedback(MpvState state)
    {
        if (!state.IsConnected || state.Number(MpvProperties.Volume) is not { } volume)
        {
            return new(Icon: VolumeIcon);
        }

        var max = state.Number(MpvProperties.VolumeMax) is { } m and > 0 ? m : DefaultVolumeMax;
        var muted = state.Flag(MpvProperties.Mute) == true;

        return new(muted ? "Muted" : FormatVolume(volume),
                   Math.Clamp(volume / max * 100.0, 0, 100),
                   muted ? MutedIcon : VolumeIcon);
    }
}
