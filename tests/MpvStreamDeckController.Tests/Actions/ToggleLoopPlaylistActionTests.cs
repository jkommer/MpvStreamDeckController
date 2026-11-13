using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class ToggleLoopPlaylistActionTests
{
    // Indexes into the action's States in manifest.json: 0 is the "off" image, 1 the "on" image.
    private const uint Off = 0;
    private const uint On = 1;

    [Theory]
    [InlineData("inf")]
    [InlineData("force")]
    [InlineData(3d)] // a fixed number of loops
    public void CreateFeedback_WhenLooping_ShowsOn(object value)
    {
        ToggleLoopPlaylistAction
            .CreateFeedback(TestStates.Connected((MpvProperties.LoopPlaylist, value)))
            .ShouldBe(new(On));
    }

    [Fact]
    public void CreateFeedback_WhenNotLooping_ShowsOff()
    {
        ToggleLoopPlaylistAction
            .CreateFeedback(TestStates.Connected((MpvProperties.LoopPlaylist, false))) // how mpv reports "no"
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenTheLoopStateIsNotKnown_ShowsOff()
    {
        ToggleLoopPlaylistAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        ToggleLoopPlaylistAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(Off));
    }
}
