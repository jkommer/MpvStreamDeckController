using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleACompressorActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the "off" image, 1 the "on" image.
    private const uint Off = 0;
    private const uint On = 1;

    [Fact]
    public void CreateFeedback_WhenTheFilterIsEnabled_ShowsOn()
    {
        ToggleACompressorAction
            .CreateFeedback(TestStates.Connected((MpvProperties.ACompressor, true)))
            .ShouldBe(new(On));
    }

    [Fact]
    public void CreateFeedback_WhenTheFilterIsDisabled_ShowsOff()
    {
        ToggleACompressorAction
            .CreateFeedback(TestStates.Connected((MpvProperties.ACompressor, false)))
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenTheFilterStateIsNotKnown_ShowsOff()
    {
        ToggleACompressorAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        ToggleACompressorAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Off));
    }
}
