using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class VolumeSettingsTests
{
    [Theory]
    [InlineData("{\"volumeStep\":\"5\"}", 5)]
    [InlineData("{\"volumeStep\":\"0.5\"}", 0.5)]
    [InlineData("{\"volumeStep\":\"0,5\"}", VolumeSettings.DefaultStep)]
    [InlineData("{\"volumeStep\":\"-1\"}", VolumeSettings.DefaultStep)]
    [InlineData("{}", VolumeSettings.DefaultStep)]
    public void Read_Step_ReturnsTheSettingOrTheDefault(string json, double expected)
    {
        VolumeSettings
            .Read(JObject.Parse(json)).Step
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        VolumeSettings
            .Read(null)
            .ShouldBeSameAs(VolumeSettings.Default);
    }

    [Fact]
    public void Read_IgnoresOtherSettings()
    {
        VolumeSettings
            .Read(JObject.Parse("{\"osd\":\"osd-bar\",\"seekInterval\":\"3\",\"volumeStep\":\"4\"}"))
            .ShouldBe(new VolumeSettings(4));
    }

    [Theory]
    [InlineData(1, 2, "[\"add\",\"volume\",2]")]
    [InlineData(-3, 2, "[\"add\",\"volume\",-6]")]
    [InlineData(3, 0.5, "[\"add\",\"volume\",1.5]")]
    [InlineData(3, 0.1, "[\"add\",\"volume\",0.3]")]
    public void CreateCommand_AddsTicksTimesTheStep(int ticks, double step, string expectedCommand)
    {
        new VolumeSettings(step)
            .CreateCommand(ticks)
            .DecodedCommandJson()
            .ShouldBe(expectedCommand);
    }
}
