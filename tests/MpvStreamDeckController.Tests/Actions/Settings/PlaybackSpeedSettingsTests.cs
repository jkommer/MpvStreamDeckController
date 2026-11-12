using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class PlaybackSpeedSettingsTests
{
    [Theory]
    [InlineData("{\"playbackSpeedStep\":\"5\"}", 5)]
    [InlineData("{\"playbackSpeedStep\":\"0.5\"}", 0.5)]
    [InlineData("{\"playbackSpeedStep\":\"0.01\"}", 0.01)]
    [InlineData("{\"playbackSpeedStep\":\"0,5\"}", PlaybackSpeedSettings.DefaultStep)]
    [InlineData("{\"playbackSpeedStep\":\"-1\"}", PlaybackSpeedSettings.DefaultStep)]
    [InlineData("{}", PlaybackSpeedSettings.DefaultStep)]
    public void Read_Step_ReturnsTheSettingOrTheDefault(string json, double expected)
    {
        PlaybackSpeedSettings
            .Read(JObject.Parse(json)).Step
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        PlaybackSpeedSettings
            .Read(null)
            .ShouldBeSameAs(PlaybackSpeedSettings.Default);
    }

    [Fact]
    public void Read_IgnoresOtherSettings()
    {
        PlaybackSpeedSettings
            .Read(JObject.Parse("{\"osd\":\"osd-bar\",\"seekInterval\":\"3\",\"playbackSpeedStep\":\"4\"}"))
            .ShouldBe(new(4));
    }

    [Theory]
    [InlineData(1, 2, "[\"add\",\"speed\",2]")]
    [InlineData(-3, 2, "[\"add\",\"speed\",-6]")]
    [InlineData(3, 0.5, "[\"add\",\"speed\",1.5]")]
    [InlineData(3, 0.1, "[\"add\",\"speed\",0.3]")]
    public void CreateCommand_AddsTicksTimesTheStep(int ticks, double step, string expectedCommand)
    {
        new PlaybackSpeedSettings(step)
            .CreateCommand(ticks)
            .DecodedCommandJson()
            .ShouldBe(expectedCommand);
    }
}
