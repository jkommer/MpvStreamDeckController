using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the custom dial action. Each command is text in input.conf syntax, as for the custom command action.
/// </summary>
/// <param name="RotateRight">The command to run for each tick the dial is turned clockwise.</param>
/// <param name="RotateLeft">The command to run for each tick the dial is turned anticlockwise.</param>
/// <param name="Press">The command to run when the dial is pressed or its touch strip is tapped.</param>
/// <param name="ExpandProperties">Whether to expand <c>${property}</c> references in every command.</param>
public sealed record CustomDialSettings(string? RotateRight = null, string? RotateLeft = null, string? Press = null, bool ExpandProperties = false)
{
    private const string RotateRightKey = "rotateRightCommand";
    private const string RotateLeftKey = "rotateLeftCommand";
    private const string PressKey = "pressCommand";
    private const string ExpandPropertiesKey = "expandProperties";

    public static CustomDialSettings Default { get; } = new();

    public static CustomDialSettings Read(JObject? settings)
    {
        if (settings is null)
        {
            return Default;
        }

        return new(RotateRight: SettingsValues.ReadString(settings, RotateRightKey),
                   RotateLeft: SettingsValues.ReadString(settings, RotateLeftKey),
                   Press: SettingsValues.ReadString(settings, PressKey),
                   ExpandProperties: SettingsValues.ReadBool(settings, ExpandPropertiesKey) ?? false);
    }

    /// <summary>
    /// Parses the three commands. Parsing happens once here, not at every tick; invalid commands are logged.
    /// </summary>
    public CustomDialCommands CreateCommands() => new(Parse(RotateRight), Parse(RotateLeft), Parse(Press));

    /// <summary>
    /// Empty if the text is blank (that part of the dial is simply not used), null if it is invalid.
    /// </summary>
    private MpvCommand[]? Parse(string? command)
        => string.IsNullOrWhiteSpace(command) ? [] : new CustomCommandSettings(command, ExpandProperties).CreateCommands();
}

/// <summary>
/// The parsed commands of a custom dial. Each is empty if nothing was entered for it, and null if what was entered is
/// not a valid command.
/// </summary>
public sealed record CustomDialCommands(MpvCommand[]? RotateRight, MpvCommand[]? RotateLeft, MpvCommand[]? Press)
{
    /// <summary>
    /// The commands for turning the dial by <paramref name="ticks"/>, anticlockwise if negative: that direction's
    /// command once per tick. Null if that direction's command is not valid.
    /// </summary>
    public MpvCommand[]? ForRotation(int ticks)
    {
        var commands = ticks < 0 ? RotateLeft : RotateRight;

        if (commands is null)
        {
            return null;
        }

        return [.. Enumerable.Repeat(commands, Math.Abs(ticks)).SelectMany(c => c)];
    }
}
