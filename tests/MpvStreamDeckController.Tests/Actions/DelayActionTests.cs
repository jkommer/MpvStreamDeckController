using System.Globalization;
using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class DelayActionTests
{
    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsAnEmptyDial()
    {
        DelayAction
            .CreateFeedback(TestStates.Disconnected, MpvProperties.SubDelay)
            .ShouldBe(new(""));
    }

    [Fact]
    public void CreateFeedback_WhenTheDelayIsNotKnownYet_ShowsAnEmptyDial()
    {
        DelayAction
            .CreateFeedback(TestStates.Connected(), MpvProperties.SubDelay)
            .ShouldBe(new(""));
    }

    [Fact]
    public void CreateFeedback_WhenConnected_ShowsTheDelay()
    {
        DelayAction
            .CreateFeedback(TestStates.Connected((MpvProperties.SubDelay, 0.15)), MpvProperties.SubDelay)
            .ShouldBe(new("+150 ms"));
    }

    [Fact]
    public void CreateFeedback_OnlyReadsItsOwnDelay()
    {
        var state = TestStates.Connected((MpvProperties.SubDelay, 0.15), (MpvProperties.AudioDelay, -0.05));

        DelayAction
            .CreateFeedback(state, MpvProperties.SubDelay)
            .ShouldBe(new("+150 ms"));

        DelayAction
            .CreateFeedback(state, MpvProperties.AudioDelay)
            .ShouldBe(new("-50 ms"));
    }

    [Theory]
    [InlineData(0, "0 ms")]
    [InlineData(0.1, "+100 ms")]
    [InlineData(-0.25, "-250 ms")]
    [InlineData(1.5, "+1500 ms")]
    [InlineData(0.30000000000000004, "+300 ms")] // what three steps of 0.1 add up to
    [InlineData(0.0004, "0 ms")]
    [InlineData(-0.0004, "0 ms")] // rounds to zero, so no sign
    public void FormatDelay_ShowsSignedWholeMilliseconds(double seconds, string expected)
    {
        DelayAction
            .FormatDelay(seconds)
            .ShouldBe(expected);
    }

    [Fact]
    public void FormatDelay_DoesNotDependOnTheLocale()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            DelayAction
                .FormatDelay(-1.5)
                .ShouldBe("-1500 ms");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
