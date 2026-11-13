using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleSubtitleVisibilityActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the "off" image, 1 the "on" image.
    private const uint Off = 0;
    private const uint On = 1;

    [Fact]
    public void CreateFeedback_WhenSubtitlesAreVisible_ShowsOn()
    {
        ToggleSubtitleVisibilityAction
            .CreateFeedback(TestStates.Connected((MpvProperties.SubVisibility, true)))
            .ShouldBe(new(On));
    }

    [Fact]
    public void CreateFeedback_WhenSubtitlesAreHidden_ShowsOff()
    {
        ToggleSubtitleVisibilityAction
            .CreateFeedback(TestStates.Connected((MpvProperties.SubVisibility, false)))
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenTheVisibilityIsNotKnown_ShowsOff()
    {
        ToggleSubtitleVisibilityAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        ToggleSubtitleVisibilityAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Off));
    }
}
