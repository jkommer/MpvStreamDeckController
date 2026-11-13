using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class BrightnessSettingsTests
{
    [Theory]
    [InlineData("{\"brightnessStep\":\"5\"}", 5)]
    [InlineData("{\"brightnessStep\":\"2.5\"}", 2.5)]
    [InlineData("{\"brightnessStep\":\"0,5\"}", BrightnessSettings.DefaultStep)]
    [InlineData("{\"brightnessStep\":\"-1\"}", BrightnessSettings.DefaultStep)]
    [InlineData("{}", BrightnessSettings.DefaultStep)]
    public void Read_Step_ReturnsTheSettingOrTheDefault(string json, double expected)
    {
        BrightnessSettings
            .Read(JObject.Parse(json)).Step
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        BrightnessSettings
            .Read(null)
            .ShouldBeSameAs(BrightnessSettings.Default);
    }

    [Fact]
    public void Read_IgnoresOtherSettings()
    {
        BrightnessSettings
            .Read(JObject.Parse("{\"osd\":\"osd-bar\",\"volumeStep\":\"3\",\"brightnessStep\":\"4\"}"))
            .ShouldBe(new(4));
    }

    [Theory]
    [InlineData(1, 1, "[\"add\",\"brightness\",1]")]
    [InlineData(-3, 1, "[\"add\",\"brightness\",-3]")]
    [InlineData(2, 2.5, "[\"add\",\"brightness\",5]")]
    [InlineData(3, 0.1, "[\"add\",\"brightness\",0.3]")]
    public void CreateCommand_AddsTicksTimesTheStep(int ticks, double step, string expectedCommand)
    {
        new BrightnessSettings(step)
            .CreateCommand(ticks)
            .DecodedCommandJson()
            .ShouldBe(expectedCommand);
    }
}
