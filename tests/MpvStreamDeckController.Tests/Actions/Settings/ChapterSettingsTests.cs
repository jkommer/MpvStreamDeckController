using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class ChapterSettingsTests
{
    [Theory]
    [InlineData("{\"enablePlaylistNavigation\":true}", true)]
    [InlineData("{\"enablePlaylistNavigation\":false}", false)]
    [InlineData("{\"enablePlaylistNavigation\":\"false\"}", ChapterSettings.DefaultEnablePlaylistNavigation)] // text, not a boolean
    [InlineData("{\"enablePlaylistNavigation\":0}", ChapterSettings.DefaultEnablePlaylistNavigation)]
    [InlineData("{\"enablePlaylistNavigation\":null}", ChapterSettings.DefaultEnablePlaylistNavigation)]
    [InlineData("{\"enablePlayListNavigation\":false}", ChapterSettings.DefaultEnablePlaylistNavigation)] // the key is case sensitive
    [InlineData("{}", ChapterSettings.DefaultEnablePlaylistNavigation)]
    public void Read_EnablePlaylistNavigation_ReturnsTheSettingOrTheDefault(string json, bool expected)
    {
        ChapterSettings
            .Read(JObject.Parse(json)).EnablePlaylistNavigation
            .ShouldBe(expected);
    }

    [Fact]
    public void Read_WithNullSettings_ReturnsDefault()
    {
        ChapterSettings
            .Read(null)
            .ShouldBeSameAs(ChapterSettings.Default);
    }

    [Fact]
    public void Read_IgnoresOtherSettings()
    {
        ChapterSettings
            .Read(JObject.Parse("{\"osd\":\"osd-bar\",\"volumeStep\":\"4\",\"enablePlaylistNavigation\":false}"))
            .ShouldBe(new ChapterSettings(false));
    }

    [Fact]
    public void Default_EnablesPlaylistNavigation()
    {
        ChapterSettings.Default.EnablePlaylistNavigation.ShouldBeTrue();
    }
}
