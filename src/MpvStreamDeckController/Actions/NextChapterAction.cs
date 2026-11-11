using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions;

[PluginActionId("com.jkommer.mpv.nextchapter")]
public class NextChapterAction(ISDConnection connection, InitialPayload payload) : MpvKeyAction(connection, payload)
{
    protected override MpvCommand[] Commands { get; } = [new("add", "chapter", 1)];
}
