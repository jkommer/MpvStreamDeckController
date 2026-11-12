using System.Globalization;
using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class PlaybackSpeedActionTests
{
    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsAnEmptyDial()
    {
        PlaybackSpeedAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(""));
    }

    [Fact]
    public void CreateFeedback_WhenThePlaybackSpeedIsNotKnownYet_ShowsAnEmptyDial()
    {
        PlaybackSpeedAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(""));
    }

    [Theory]
    [InlineData(0.5, "0.50x")]
    [InlineData(1, "1.00x")]
    [InlineData(1.5, "1.50x")]
    public void CreateFeedback_WhenConnected_ShowsThePlaybackSpeed(double playbackSpeed, string text)
    {
        PlaybackSpeedAction
            .CreateFeedback(TestStates.Connected((MpvProperties.PlaybackSpeed, playbackSpeed)))
            .ShouldBe(new(text));
    }

    [Theory]
    [InlineData(0.01, "0.01x")]
    [InlineData(0.15, "0.15x")]
    [InlineData(1, "1.00x")]
    [InlineData(42.42, "42.42x")]
    [InlineData(100, "100.00x")]
    public void FormatPlaybackSpeed_AlwaysShowsTwoDecimals(double playbackSpeed, string expected)
    {
        PlaybackSpeedAction
            .FormatPlaybackSpeed(playbackSpeed)
            .ShouldBe(expected);
    }

    [Fact]
    public void FormatPlaybackSpeed_UsesAFullStopWhateverTheLocale()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            PlaybackSpeedAction
                .FormatPlaybackSpeed(1.1)
                .ShouldBe("1.10x");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
