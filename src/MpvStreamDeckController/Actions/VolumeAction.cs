using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.volume")]
public class VolumeAction(ISDConnection connection, InitialPayload payload) : MpvDialAction(connection, payload, [MpvProperties.Volume, MpvProperties.VolumeMax, MpvProperties.Mute])
{
    private const int PercentPerTick = 2;
    private const double DefaultVolumeMax = 100;

    // Paths relative to the plugin folder; must match the files you ship.
    private const string VolumeIcon = "Images/dial/11a_volume_dial";
    private const string MutedIcon = "Images/dial/11b_volume_muted_dial";

    // mpv clamps "add volume" to [0, volume-max], so no range handling is needed here.
    protected override MpvCommand[] RotateCommands(int ticks) => [new("add", "volume", ticks * PercentPerTick)];

    protected override MpvCommand[] PressCommands => [new("cycle", "mute")];

    protected override DialFeedback GetFeedback(MpvState state)
    {
        if (!state.IsConnected || state.Number(MpvProperties.Volume) is not { } volume)
        {
            return new(Icon: VolumeIcon);
        }

        var max = state.Number(MpvProperties.VolumeMax) is { } m and > 0 ? m : DefaultVolumeMax;
        var muted = state.Flag(MpvProperties.Mute) == true;

        return new(muted ? "Muted" : $"{volume:0}%",
                   Math.Clamp(volume / max * 100.0, 0, 100),
                   muted ? MutedIcon : VolumeIcon);
    }
}