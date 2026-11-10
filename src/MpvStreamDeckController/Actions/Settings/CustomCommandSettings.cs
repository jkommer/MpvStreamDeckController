using BarRaider.SdTools;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the custom command action.
/// </summary>
/// <param name="Command">The command text as typed in input.conf syntax, e.g. <c>seek 10; show-text "${time-pos}"</c>.</param>
/// <param name="ExpandProperties">Whether to expand <c>${property}</c> references in every command.</param>
public sealed record CustomCommandSettings(string? Command = null, bool ExpandProperties = false)
{
    private const string CommandKey = "command";
    private const string ExpandPropertiesKey = "expandProperties";

    public static CustomCommandSettings Default { get; } = new();

    public static CustomCommandSettings Read(JObject? settings)
    {
        if (settings is null)
        {
            return Default;
        }

        return new(Command: SettingsValues.ReadString(settings, CommandKey),
                   ExpandProperties: SettingsValues.ReadBool(settings, ExpandPropertiesKey) ?? false);
    }

    /// <summary>
    /// Parses <see cref="Command"/> into the commands to send, or null if it is empty or invalid (invalid commands are logged).
    /// </summary>
    public MpvCommand[]? CreateCommands()
    {
        if (MpvCommandParser.TryParse(Command, out var result, out var error))
        {
            return [.. result.Commands.Select(x => new MpvCommand([.. x]) { ExpandProperties = ExpandProperties })];
        }

        if (!string.IsNullOrWhiteSpace(Command))
        {
            LogHelper.Log(TracingLevel.ERROR, $"Custom command is not valid: {Command}, error: {error}");
        }

        return null;
    }
}
