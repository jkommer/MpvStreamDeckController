using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Settings for the previous and next chapter actions.
/// </summary>
/// <param name="EnablePlaylistNavigation">Whether to navigate back/forward through the playlist upon reaching the first/final chapter of a file respectively.</param>
public sealed record ChapterSettings(bool EnablePlaylistNavigation = ChapterSettings.DefaultEnablePlaylistNavigation)
{
    public const bool DefaultEnablePlaylistNavigation = true;
    private const string EnablePlaylistNavigationKey = "enablePlaylistNavigation";

    public static ChapterSettings Default { get; } = new();

    public static ChapterSettings Read(JObject? settings)
    {
        if (settings is null)
        {
            return Default;
        }

        return new(SettingsValues.ReadBool(settings, EnablePlaylistNavigationKey) ?? DefaultEnablePlaylistNavigation);
    }
}
