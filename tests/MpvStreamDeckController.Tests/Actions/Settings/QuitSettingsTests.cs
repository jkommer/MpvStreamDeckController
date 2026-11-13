using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class QuitSettingsTests
{
    [Theory]
    [InlineData("{\"savePosition\":true}", true)]
    [InlineData("{\"savePosition\":false}", false)]
    [InlineData("{\"savePosition\":\"true\"}", QuitSettings.DefaultSavePosition)] // text, not a boolean
    [InlineData("{\"savePosition\":null}", QuitSettings.DefaultSavePosition)]
    [InlineData("{}", QuitSettings.DefaultSavePosition)]
    public void Read_SavePosition_ReturnsTheSettingOrTheDefault(string json, bool expected)
    {
        QuitSettings
            .Read(JObject.Parse(json)).SavePosition
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        QuitSettings
            .Read(null)
            .ShouldBeSameAs(QuitSettings.Default);
    }

    [Theory]
    [InlineData(false, "[\"quit\"]")]
    [InlineData(true, "[\"quit-watch-later\"]")]
    public void CreateCommand_SavesThePositionOnlyWhenAsked(bool savePosition, string expectedCommand)
    {
        new QuitSettings(savePosition)
            .CreateCommand()
            .DecodedCommandJson()
            .ShouldBe(expectedCommand);
    }
}
