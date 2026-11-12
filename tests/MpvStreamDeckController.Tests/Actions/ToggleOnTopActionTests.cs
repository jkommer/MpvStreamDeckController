using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleOnTopActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the "off" image, 1 the "on" image.
    private const uint Off = 0;
    private const uint On = 1;

    [Fact]
    public void CreateFeedback_WhenOnTopIsEnabled_ShowsOn()
    {
        ToggleOnTopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.OnTop, true)))
            .ShouldBe(new(On));
    }

    [Fact]
    public void CreateFeedback_WhenOnTopIsDisabled_ShowsOff()
    {
        ToggleOnTopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.OnTop, false)))
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenTheOnTopStateIsNotKnown_ShowsOff()
    {
        ToggleOnTopAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        ToggleOnTopAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Off));
    }
}
