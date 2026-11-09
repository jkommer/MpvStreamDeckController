using MpvStreamDeckController.Actions;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests;

public class CommonActionSettingsTests
{
    [Theory]
    [InlineData("{\"osd\":\"osd-msg\"}", "osd-msg")]
    [InlineData("{\"osd\":\"no-osd\"}", "no-osd")]
    [InlineData("{\"osd\":\"osd-msg-bar\",\"command\":\"cycle pause\"}", "osd-msg-bar")]
    [InlineData("{\"osd\":\"\"}", null)]
    [InlineData("{\"osd\":\"bogus\"}", null)]
    [InlineData("{\"osd\":1}", null)]
    [InlineData("{\"osd\":{}}", null)]
    [InlineData("{}", null)]
    public void Read_Osd_ReturnsConfiguredModeOrNull(string json, string? expectedPrefix)
    {
        var osd = CommonActionSettings.Read(JObject.Parse(json)).Osd;

        osd?.Prefix.ShouldBe(expectedPrefix);
        (osd is null).ShouldBe(expectedPrefix is null);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"osd\":\"bogus\"}")]
    public void Read_WithNothingConfigured_EqualsDefault(string json)
    {
        CommonActionSettings.Read(JObject.Parse(json)).ShouldBe(CommonActionSettings.Default);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        CommonActionSettings.Read(null).ShouldBeSameAs(CommonActionSettings.Default);
    }

    [Fact]
    public void Apply_WithOsd_SetsItOnEveryCommandAndKeepsOtherOptions()
    {
        MpvCommand[] commands = [new("cycle", "pause") { ExpandProperties = true }, new("seek", 5) { Osd = MpvOsdMode.OsdAuto }];

        var result = new CommonActionSettings(Osd: MpvOsdMode.OsdBar).Apply(commands);

        result.ShouldNotBeNull();
        result.ShouldAllBe(command => command.Osd == MpvOsdMode.OsdBar);
        result[0].ExpandProperties.ShouldBeTrue();
        result[0].Components.ShouldBe(commands[0].Components);
    }

    [Fact]
    public void Apply_WithDefaultSettings_ReturnsCommandsUnchanged()
    {
        MpvCommand[] commands = [new("cycle", "pause") { Osd = MpvOsdMode.NoOsd }];

        CommonActionSettings.Default.Apply(commands).ShouldBeSameAs(commands);
        CommonActionSettings.Default.Apply(commands[0]).ShouldBeSameAs(commands[0]);
    }

    [Fact]
    public void Apply_WithNullCommands_ReturnsNull()
    {
        new CommonActionSettings(Osd: MpvOsdMode.OsdMsg).Apply((MpvCommand[]?)null).ShouldBeNull();
    }
}
