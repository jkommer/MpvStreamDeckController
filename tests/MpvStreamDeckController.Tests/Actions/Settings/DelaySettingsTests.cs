using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class DelaySettingsTests
{
    [Theory]
    [InlineData("{\"delayStep\":\"50\"}", 50)]
    [InlineData("{\"delayStep\":\"12.5\"}", 12.5)]
    [InlineData("{\"delayStep\":\"0,5\"}", DelaySettings.DefaultStepMilliseconds)]
    [InlineData("{\"delayStep\":\"-1\"}", DelaySettings.DefaultStepMilliseconds)]
    [InlineData("{}", DelaySettings.DefaultStepMilliseconds)]
    public void Read_Step_ReturnsTheSettingOrTheDefault(string json, double expected)
    {
        DelaySettings
            .Read(JObject.Parse(json)).StepMilliseconds
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        DelaySettings
            .Read(null)
            .ShouldBeSameAs(DelaySettings.Default);
    }

    [Fact]
    public void Read_IgnoresOtherSettings()
    {
        DelaySettings
            .Read(JObject.Parse("{\"osd\":\"osd-bar\",\"volumeStep\":\"3\",\"delayStep\":\"40\"}"))
            .ShouldBe(new(40));
    }

    [Theory]
    [InlineData("sub-delay", 1, 100, "[\"add\",\"sub-delay\",0.1]")]
    [InlineData("sub-delay", -3, 100, "[\"add\",\"sub-delay\",-0.3]")]
    [InlineData("audio-delay", 2, 25, "[\"add\",\"audio-delay\",0.05]")]
    [InlineData("audio-delay", 3, 1, "[\"add\",\"audio-delay\",0.003]")]
    public void CreateCommand_AddsTicksTimesTheStepInSeconds(string property, int ticks, double stepMilliseconds, string expectedCommand)
    {
        new DelaySettings(stepMilliseconds)
            .CreateCommand(property, ticks)
            .DecodedCommandJson()
            .ShouldBe(expectedCommand);
    }
}
