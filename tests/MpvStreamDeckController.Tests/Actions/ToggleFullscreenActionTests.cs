using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleFullscreenActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the "off" image, 1 the "on" image.
    private const uint Off = 0;
    private const uint On = 1;

    [Fact]
    public void CreateFeedback_WhenFullscreen_ShowsOn()
    {
        ToggleFullscreenAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Fullscreen, true)))
            .ShouldBe(new(On));
    }

    [Fact]
    public void CreateFeedback_WhenNotFullscreen_ShowsOff()
    {
        ToggleFullscreenAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Fullscreen, false)))
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenTheFullscreenStateIsNotKnown_ShowsOff()
    {
        ToggleFullscreenAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        ToggleFullscreenAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Off));
    }
}
