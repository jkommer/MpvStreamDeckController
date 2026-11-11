using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.nextchapter")]
public class NextChapterAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload, [MpvProperties.Chapter, MpvProperties.Chapters])
{
    private volatile ChapterSettings _settings = ChapterSettings.Read(payload.Settings);

    protected override MpvCommand[] Commands => CreateCommands(_settings, State);

    /// <summary>
    /// The next chapter, or the next file when playlist navigation is on and there is no next chapter (or the file has no chapters).
    /// Separate from the action so it can be tested without a Stream Deck connection.
    /// </summary>
    internal static MpvCommand[] CreateCommands(ChapterSettings settings, MpvState state)
    {
        var hasChapterToGoTo = state.Number(MpvProperties.Chapter) is { } chapter
            && state.Number(MpvProperties.Chapters) is { } chapters
            && chapter < chapters - 1;

        return !settings.EnablePlaylistNavigation || hasChapterToGoTo
            ? [new("add", "chapter", 1)]
            : [new("playlist-next")];
    }

    protected override void OnSettingsChanged(JObject? settings) => _settings = ChapterSettings.Read(settings);
}
