using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class AbLoopActionTests
{
    [Fact]
    public void CreateFeedback_WithBothPoints_ShowsLooping()
    {
        AbLoopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.AbLoopA, 10d), (MpvProperties.AbLoopB, 20d)))
            .ShouldBe(new(AbLoopAction.Looping));
    }

    [Fact]
    public void CreateFeedback_WithOnlyTheStart_ShowsTheStartSet()
    {
        AbLoopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.AbLoopA, 10d), (MpvProperties.AbLoopB, "no")))
            .ShouldBe(new(AbLoopAction.StartSet));
    }

    [Fact]
    public void CreateFeedback_WithOnlyTheEnd_ShowsTheStartSet()
    {
        // Not reachable with the key, but the points can be set separately elsewhere; it is still one point of two.
        AbLoopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.AbLoopA, "no"), (MpvProperties.AbLoopB, 20d)))
            .ShouldBe(new(AbLoopAction.StartSet));
    }

    [Fact]
    public void CreateFeedback_WithAPointAtZero_CountsItAsSet()
    {
        AbLoopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.AbLoopA, 0d), (MpvProperties.AbLoopB, 20d)))
            .ShouldBe(new(AbLoopAction.Looping));
    }

    [Theory]
    [InlineData("no")]
    [InlineData(false)]
    public void CreateFeedback_WithNoPoints_ShowsOff(object unset)
    {
        AbLoopAction
            .CreateFeedback(TestStates.Connected((MpvProperties.AbLoopA, unset), (MpvProperties.AbLoopB, unset)))
            .ShouldBe(new(AbLoopAction.Off));
    }

    [Fact]
    public void CreateFeedback_WhenThePointsAreNotKnown_ShowsOff()
    {
        AbLoopAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(AbLoopAction.Off));
    }

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsOff()
    {
        AbLoopAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(AbLoopAction.Off));
    }
}
