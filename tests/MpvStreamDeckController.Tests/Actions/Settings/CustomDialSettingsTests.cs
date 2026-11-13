using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class CustomDialSettingsTests
{
    [Fact]
    public void Read_ReturnsTheSettings()
    {
        CustomDialSettings
            .Read(JObject.Parse("{\"rotateRightCommand\":\"add volume 2\",\"rotateLeftCommand\":\"add volume -2\",\"pressCommand\":\"cycle mute\",\"expandProperties\":true,\"osd\":\"osd-bar\"}"))
            .ShouldBe(new("add volume 2", "add volume -2", "cycle mute", true));
    }

    [Fact]
    public void Read_WithMissingOrWronglyTypedValues_LeavesThemUnset()
    {
        CustomDialSettings
            .Read(JObject.Parse("{\"rotateRightCommand\":{},\"expandProperties\":\"yes\"}"))
            .ShouldBe(new(null, null, null, false));
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        CustomDialSettings.Read(null).ShouldBeSameAs(CustomDialSettings.Default);
    }

    [Theory]
    [InlineData(1, new[] { "add volume 2" })]
    [InlineData(3, new[] { "add volume 2", "add volume 2", "add volume 2" })]
    [InlineData(-1, new[] { "add volume -2" })]
    [InlineData(-2, new[] { "add volume -2", "add volume -2" })]
    [InlineData(0, new string[0])]
    public void ForRotation_SendsThatDirectionsCommandOncePerTick(int ticks, string[] expected)
    {
        var commands = new CustomDialSettings("add volume 2", "add volume -2", "cycle mute").CreateCommands();

        commands.ForRotation(ticks)
            .ShouldNotBeNull()
            .Select(c => string.Join(" ", c.Components))
            .ShouldBe(expected);
    }

    [Fact]
    public void ForRotation_WithSeveralCommands_RepeatsThemInOrder()
    {
        var commands = new CustomDialSettings("add brightness 1; show-text hi").CreateCommands();

        commands.ForRotation(2)
            .ShouldNotBeNull()
            .Select(c => string.Join(" ", c.Components))
            .ShouldBe(["add brightness 1", "show-text hi", "add brightness 1", "show-text hi"]);
    }

    [Fact]
    public void CreateCommands_ParsesThePressCommand()
    {
        var commands = new CustomDialSettings(Press: "cycle mute; show-text muted").CreateCommands();

        commands.Press
            .ShouldNotBeNull()
            .Select(c => c.Components)
            .ShouldBe([["cycle", "mute"], ["show-text", "muted"]]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCommands_WhenACommandIsBlank_ItIsEmptyRatherThanInvalid(string? blank)
    {
        var commands = new CustomDialSettings(blank, blank, blank).CreateCommands();

        commands.Press.ShouldBe([]);

        commands
            .ForRotation(2)
            .ShouldBe([]);

        commands
            .ForRotation(-2)
            .ShouldBe([]);
    }

    [Fact]
    public void CreateCommands_WhenACommandIsInvalid_OnlyThatOneIsNull()
    {
        var commands = new CustomDialSettings("add volume 2", "show-text \"unterminated", "cycle mute").CreateCommands();

        commands
            .ForRotation(1)
            .ShouldNotBeNull();

        commands
            .ForRotation(-1)
            .ShouldBeNull();

        commands.Press.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateCommands_AppliesExpandPropertiesToEveryCommand(bool expandProperties)
    {
        var commands = new CustomDialSettings("add volume 2", "add volume -2", "show-text \"${volume}\"", expandProperties).CreateCommands();

        commands
            .ForRotation(1)
            .ShouldNotBeNull()
            .ShouldAllBe(c => c.ExpandProperties == expandProperties);

        commands
            .ForRotation(-1)
            .ShouldNotBeNull()
            .ShouldAllBe(c => c.ExpandProperties == expandProperties);

        commands.Press
            .ShouldNotBeNull()
            .ShouldAllBe(c => c.ExpandProperties == expandProperties);
    }
}
