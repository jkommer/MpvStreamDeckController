using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class FrameStepActionTests
{
    [Theory]
    [InlineData(1, "frame-step", 1)]
    [InlineData(3, "frame-step", 3)]
    [InlineData(-1, "frame-back-step", 1)]
    [InlineData(-2, "frame-back-step", 2)]
    [InlineData(0, "frame-step", 0)]
    public void CreateCommands_SendsOneStepPerTick(int ticks, string expectedCommand, int expectedCount)
    {
        FrameStepAction
            .CreateCommands(ticks)
            .Select(command => command.DecodedCommandJson())
            .ShouldBe(Enumerable.Repeat($"[\"{expectedCommand}\"]", expectedCount));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_IsEmpty()
    {
        FrameStepAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(default);
    }

    [Fact]
    public void CreateFeedback_WhenNothingIsPlaying_IsEmpty()
    {
        // Connected and paused, but mpv has no time-pos while idle.
        FrameStepAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Pause, true), (MpvProperties.ContainerFps, 24d)))
            .ShouldBe(default);
    }

    [Theory]
    [InlineData(75, 24, "#1800")]
    [InlineData(75.0417, 24, "#1801")] // one frame on
    [InlineData(75.0834, 24, "#1802")]
    [InlineData(75.9, 23.976, "#1820")]
    public void CreateFeedback_WhenPaused_ShowsTheFrameNumberAndKeepsTheBar(double exactTime, double framesPerSecond, string text)
    {
        var feedback = FrameStepAction.CreateFeedback(TestStates.Connected(
            (MpvProperties.TimePos, 75d),
            (MpvProperties.Duration, 300d),
            (MpvProperties.Pause, true),
            (MpvProperties.ExactTimePos, exactTime),
            (MpvProperties.ContainerFps, framesPerSecond)));

        feedback.Text.ShouldBe(text);
        feedback.Progress.ShouldBe(25, 0.001);
        feedback.Icon.ShouldBeNull();
    }

    [Fact]
    public void CreateFeedback_WhilePlaying_ShowsThePositionLikeTheSeekDial()
    {
        var state = TestStates.Connected(
            (MpvProperties.TimePos, 75d),
            (MpvProperties.Duration, 300d),
            (MpvProperties.Pause, false),
            (MpvProperties.ExactTimePos, 75.5),
            (MpvProperties.ContainerFps, 24d));

        var feedback = FrameStepAction.CreateFeedback(state);

        feedback.ShouldBe(SeekAction.CreateFeedback(state));
        feedback.Text.ShouldBe("01:15");
    }

    [Fact]
    public void CreateFeedback_WhenPausedButTheFrameRateIsNotKnown_ShowsThePosition()
    {
        var feedback = FrameStepAction.CreateFeedback(TestStates.Connected(
            (MpvProperties.TimePos, 75d),
            (MpvProperties.Duration, 300d),
            (MpvProperties.Pause, true),
            (MpvProperties.ExactTimePos, 75.5)));

        feedback.Text.ShouldBe("01:15");
    }

    [Fact]
    public void CreateFeedback_WhenPausedWithAFrameRateOfZero_ShowsThePosition()
    {
        var feedback = FrameStepAction.CreateFeedback(TestStates.Connected(
            (MpvProperties.TimePos, 75d),
            (MpvProperties.Duration, 300d),
            (MpvProperties.Pause, true),
            (MpvProperties.ExactTimePos, 75.5),
            (MpvProperties.ContainerFps, 0d)));

        feedback.Text.ShouldBe("01:15");
    }

    [Theory]
    [InlineData(0, "#0")]
    [InlineData(1234, "#1234")]
    [InlineData(1234567, "#1234567")]
    public void FormatFrame_ShowsAWholeNumberWithoutSeparators(double frame, string expected)
    {
        FrameStepAction
            .FormatFrame(frame)
            .ShouldBe(expected);
    }
}
