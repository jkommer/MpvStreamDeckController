using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the quit action.
/// </summary>
/// <param name="SavePosition">Whether to have mpv remember the playback position, so the file resumes there next time.</param>
public sealed record QuitSettings(bool SavePosition = QuitSettings.DefaultSavePosition)
{
    public const bool DefaultSavePosition = false;
    private const string SavePositionKey = "savePosition";

    public static QuitSettings Default { get; } = new();

    public static QuitSettings Read(JObject? settings)
    {
        if (settings is null)
        {
            return Default;
        }

        return new(SettingsValues.ReadBool(settings, SavePositionKey) ?? DefaultSavePosition);
    }

    /// <summary>
    /// quit-watch-later saves the position before quitting. Plain quit leaves that to mpv's own save-position-on-quit option.
    /// </summary>
    public MpvCommand CreateCommand() => new(SavePosition ? "quit-watch-later" : "quit");
}
