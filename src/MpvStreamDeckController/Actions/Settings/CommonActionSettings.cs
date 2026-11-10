using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings shared by every action. <see cref="Apply(MpvCommand)"/> these settings to commands before sending them.
/// To add a shared setting: add a property, read it in <see cref="Read"/>, and apply it in <see cref="Apply(MpvCommand)"/>.
/// </summary>
/// <param name="Osd">OSD mode to send commands with; null uses the mpv default</param>
public sealed record CommonActionSettings(MpvOsdMode? Osd = null)
{
    private const string OsdKey = "osd";
    public static CommonActionSettings Default { get; } = new();

    /// <summary>
    /// Reads the shared settings from an action's settings object.
    /// </summary>
    public static CommonActionSettings Read(JObject? settings)
    {
        if (settings is null)
        {
            return Default;
        }

        return new(Osd: MpvOsdMode.TryParse(SettingsValues.ReadString(settings, OsdKey), out var osd) ? osd : null);
    }

    public MpvCommand Apply(MpvCommand command)
    {
        if (Osd is not null)
        {
            command = command with { Osd = Osd };
        }

        return command;
    }

    public MpvCommand[]? Apply(MpvCommand[]? commands) => commands is null || this == Default ? commands : [.. commands.Select(Apply)];
}
