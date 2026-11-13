using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleMuteActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the normal speaker image, 1 the muted image.
    private const uint Unmuted = 0;
    private const uint Muted = 1;

    [Fact]
    public void CreateFeedback_WhenMuted_ShowsMuted()
    {
        ToggleMuteAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Mute, true)))
            .ShouldBe(new(Muted));
    }

    [Fact]
    public void CreateFeedback_WhenNotMuted_ShowsUnmuted()
    {
        ToggleMuteAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Mute, false)))
            .ShouldBe(new(Unmuted));
    }

    [Fact]
    public void CreateFeedback_WhenTheMuteStateIsNotKnown_ShowsUnmuted()
    {
        ToggleMuteAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Unmuted));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsUnmuted()
    {
        ToggleMuteAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Unmuted));
    }
}
