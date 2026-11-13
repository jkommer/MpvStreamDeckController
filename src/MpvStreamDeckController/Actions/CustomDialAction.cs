using BarRaider.SdTools;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions;

/// <summary>
/// A dial that sends the user's own commands: one for each tick clockwise, one for each tick anticlockwise, and one
/// for a press or tap. It shows nothing from mpv; choosing an OSD mode makes mpv show the result on screen instead.
/// </summary>
[PluginActionId("com.jkommer.mpv.customdial")]
public class CustomDialAction(ISDConnection connection, InitialPayload payload) : MpvDialAction(connection, payload, [])
{
    private volatile CustomDialCommands _commands = CustomDialSettings.Read(payload.Settings).CreateCommands();

    // An invalid command throws, which the base class logs and shows as the alert. A blank one sends nothing.
    protected override MpvCommand[] RotateCommands(int ticks)
        => _commands.ForRotation(ticks) ?? throw new InvalidOperationException("The rotate command for this direction is not valid.");

    protected override MpvCommand[] PressCommands
        => _commands.Press ?? throw new InvalidOperationException("The press command is not valid.");

    protected override void OnSettingsChanged(JObject? settings) => _commands = CustomDialSettings.Read(settings).CreateCommands();

    // Never called: the action observes no properties.
    protected override DialFeedback GetFeedback(MpvState state) => default;
}
