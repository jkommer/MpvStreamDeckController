using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.previouschapter")]
public class PreviousChapterAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override MpvCommand[] Commands { get; } = [new("add", "chapter", -1)];
}
