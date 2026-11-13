using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleInterpolationActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the "off" image, 1 the "on" image.
    private const uint Off = 0;
    private const uint On = 1;

    [Fact]
    public void CreateFeedback_WhenInterpolating_ShowsOn()
    {
        ToggleInterpolationAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Interpolation, true)))
            .ShouldBe(new(On));
    }

    [Fact]
    public void CreateFeedback_WhenNotInterpolating_ShowsOff()
    {
        ToggleInterpolationAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Interpolation, false)))
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenTheInterpolationStateIsNotKnown_ShowsOff()
    {
        ToggleInterpolationAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        ToggleInterpolationAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Off));
    }
}
