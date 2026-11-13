using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class PlayPauseActionTests
{
    [Fact]
    public void CreateFeedback_WhenPaused_ShowsPaused()
    {
        PlayPauseAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Pause, true)))
            .ShouldBe(new(PlayPauseAction.Paused));
    }

    [Fact]
    public void CreateFeedback_WhenNotPaused_ShowsPlaying()
    {
        PlayPauseAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Pause, false)))
            .ShouldBe(new(PlayPauseAction.Playing));
    }

    [Fact]
    public void CreateFeedback_WhenThePauseStateIsNotKnown_ShowsPlaying()
    {
        PlayPauseAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(PlayPauseAction.Playing));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsPlaying()
    {
        PlayPauseAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(PlayPauseAction.Playing));
    }
}
