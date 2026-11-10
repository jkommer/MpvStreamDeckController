using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class CustomCommandSettingsTests
{
    [Theory]
    [InlineData("{\"command\":\"cycle pause\",\"expandProperties\":true}", "cycle pause", true)]
    [InlineData("{\"command\":\"cycle pause\",\"expandProperties\":false}", "cycle pause", false)]
    [InlineData("{\"command\":\"cycle pause\"}", "cycle pause", false)]
    [InlineData("{\"command\":\"cycle pause\",\"expandProperties\":\"yes\"}", "cycle pause", false)]
    [InlineData("{\"command\":{},\"expandProperties\":true}", null, true)]
    [InlineData("{\"osd\":\"osd-bar\"}", null, false)]
    public void Read_ReturnsTheSettings(string json, string? expectedCommand, bool expectedExpandProperties)
    {
        CustomCommandSettings.Read(JObject.Parse(json)).ShouldBe(new CustomCommandSettings(expectedCommand, expectedExpandProperties));
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        CustomCommandSettings.Read(null).ShouldBeSameAs(CustomCommandSettings.Default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateCommands_ParsesEveryCommandAndAppliesExpandProperties(bool expandProperties)
    {
        var commands = new CustomCommandSettings("seek 10; show-text \"${time-pos}\"", expandProperties).CreateCommands();

        commands.ShouldNotBeNull();
        commands.Select(c => c.Components).ShouldBe([["seek", "10"], ["show-text", "${time-pos}"]]);
        commands.ShouldAllBe(c => c.ExpandProperties == expandProperties);
        commands.ShouldAllBe(c => c.Osd == null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("show-text \"unterminated")]
    public void CreateCommands_WhenEmptyOrInvalid_ReturnsNull(string? command)
    {
        new CustomCommandSettings(command, true).CreateCommands().ShouldBeNull();
    }
}
