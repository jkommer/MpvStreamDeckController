using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.previouschapter")]
public class PreviousChapterAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override object[][] Commands { get; } = [["add", "chapter", -1]];
}
