using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class SeekSettingsTests
{
    // Number parsing itself is covered by SettingsValuesTests.
    [Theory]
    [InlineData("{\"seekInterval\":\"0.5\"}", 0.5)]
    [InlineData("{\"seekInterval\":\"0,5\"}", SeekSettings.DefaultInterval)]
    [InlineData("{}", SeekSettings.DefaultInterval)]
    public void Read_Interval_ReturnsTheSettingOrTheDefault(string json, double expected)
    {
        SeekSettings
            .Read(JObject.Parse(json)).Interval
            .ShouldBe(expected);
    }

    [Theory]
    [InlineData("{\"seekInterval\":\"30\"}", 30)]
    [InlineData("{\"seekInterval\":\"0,5\"}", SeekSettings.DefaultKeyInterval)]
    [InlineData("{}", SeekSettings.DefaultKeyInterval)]
    public void Read_Interval_WithADefaultInterval_ReturnsTheSettingOrThatDefault(string json, double expected)
    {
        SeekSettings
            .Read(JObject.Parse(json), SeekSettings.DefaultKeyInterval).Interval
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettingsAndADefaultInterval_UsesThatDefault()
    {
        SeekSettings
            .Read(null, SeekSettings.DefaultKeyInterval)
            .ShouldBe(new(SeekSettings.DefaultKeyInterval));
    }

    [Theory]
    [InlineData("{\"seekType\":\"exact\"}", "exact")]
    [InlineData("{\"seekType\":\"keyframes\"}", "keyframes")]
    [InlineData("{\"seekType\":\"\"}", null)]
    [InlineData("{\"seekType\":\"bogus\"}", null)]
    [InlineData("{\"seekType\":1}", null)]
    [InlineData("{}", null)]
    public void Read_Type_ReturnsConfiguredTypeOrNull(string json, string? expectedFlag)
    {
        var seekSettings = SeekSettings.Read(JObject.Parse(json));

        seekSettings.Type?.Flag.ShouldBe(expectedFlag);
        (seekSettings.Type is null).ShouldBe(expectedFlag is null);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        SeekSettings
            .Read(null)
            .ShouldBeSameAs(SeekSettings.Default);
    }

    [Fact]
    public void Read_IgnoresOtherSettings()
    {
        SeekSettings.Read(JObject.Parse("{\"osd\":\"osd-bar\",\"seekInterval\":\"3\",\"seekType\":\"exact\"}"))
            .ShouldBe(new SeekSettings(3, MpvSeekType.Exact));
    }

    [Theory]
    [InlineData(1, 1, "[\"seek\",1,\"relative\"]")]
    [InlineData(-2, 1, "[\"seek\",-2,\"relative\"]")]
    [InlineData(3, 0.5, "[\"seek\",1.5,\"relative\"]")]
    [InlineData(3, 0.1, "[\"seek\",0.3,\"relative\"]")]
    [InlineData(-1, 10, "[\"seek\",-10,\"relative\"]")]
    public void CreateCommand_SeeksTicksTimesTheInterval(int ticks, double interval, string expectedCommand)
    {
        new SeekSettings(interval)
            .CreateCommand(ticks)
            .DecodedCommandJson()
            .ShouldBe(expectedCommand);
    }

    [Fact]
    public void CreateCommand_WithASeekType_AddsItsFlag()
    {
        new SeekSettings(1, MpvSeekType.Exact)
            .CreateCommand(1)
            .DecodedCommandJson()
            .ShouldBe("[\"seek\",1,\"relative+exact\"]");

        new SeekSettings(1, MpvSeekType.Keyframes)
            .CreateCommand(-1)
            .DecodedCommandJson()
            .ShouldBe("[\"seek\",-1,\"relative+keyframes\"]");
    }
}
