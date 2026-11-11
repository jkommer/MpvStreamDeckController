using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class SeekActionTests
{
    [Fact]
    public void CreateFeedback_WhenDisconnected_IsEmpty()
    {
        SeekAction.CreateFeedback(TestStates.Disconnected).ShouldBe(default);
    }

    [Fact]
    public void CreateFeedback_WhenNothingIsPlaying_IsEmpty()
    {
        // Connected, but mpv has no time-pos while idle.
        SeekAction.CreateFeedback(TestStates.Connected((MpvProperties.Duration, 300d))).ShouldBe(default);
    }

    [Theory]
    [InlineData(0, 300, "00:00", 0)]
    [InlineData(75, 300, "01:15", 25)]
    [InlineData(300, 300, "05:00", 100)]
    [InlineData(3599, 3599, "59:59", 100)]
    public void CreateFeedback_UnderAnHour_ShowsMinutesAndSeconds(double time, double duration, string text, double progress)
    {
        var feedback = SeekAction.CreateFeedback(TestStates.Connected((MpvProperties.TimePos, time), (MpvProperties.Duration, duration)));

        feedback.Text.ShouldBe(text);
        feedback.Progress.ShouldBe(progress, 0.001);
        feedback.Icon.ShouldBeNull();
    }

    [Theory]
    [InlineData(3725, 7200, "1:02:05")]
    [InlineData(5, 7200, "0:00:05")] // a long file shows hours from the start, so the text doesn't change width part-way
    [InlineData(3600, 0, "1:00:00")]
    public void CreateFeedback_WhenThePositionOrDurationReachesAnHour_ShowsHours(double time, double duration, string text)
    {
        SeekAction.CreateFeedback(TestStates.Connected((MpvProperties.TimePos, time), (MpvProperties.Duration, duration))).Text.ShouldBe(text);
    }

    [Fact]
    public void CreateFeedback_WithoutADuration_ShowsThePositionAndNoProgress()
    {
        // e.g. a live stream
        var feedback = SeekAction.CreateFeedback(TestStates.Connected((MpvProperties.TimePos, 65d)));

        feedback.Text.ShouldBe("01:05");
        feedback.Progress.ShouldBe(0);
    }

    [Fact]
    public void CreateFeedback_WithANegativePosition_ShowsZero()
    {
        var feedback = SeekAction.CreateFeedback(TestStates.Connected((MpvProperties.TimePos, -2d), (MpvProperties.Duration, 300d)));

        feedback.Text.ShouldBe("00:00");
        feedback.Progress.ShouldBe(0);
    }

    [Fact]
    public void CreateFeedback_WhenThePositionIsPastTheDuration_CapsProgressAt100()
    {
        SeekAction.CreateFeedback(TestStates.Connected((MpvProperties.TimePos, 310d), (MpvProperties.Duration, 300d))).Progress.ShouldBe(100);
    }
}
