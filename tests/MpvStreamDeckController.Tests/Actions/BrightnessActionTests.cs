using System.Globalization;
using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class BrightnessActionTests
{
    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsAnEmptyDial()
    {
        BrightnessAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new(""));
    }

    [Fact]
    public void CreateFeedback_WhenTheBrightnessIsNotKnownYet_ShowsAnEmptyDial()
    {
        BrightnessAction
            .CreateFeedback(TestStates.Connected())
            .ShouldBe(new(""));
    }

    [Theory]
    [InlineData(-100, "-100", 0)]
    [InlineData(-50, "-50", 25)]
    [InlineData(0, "0", 50)] // an unchanged picture sits in the middle of the bar
    [InlineData(5, "+5", 52.5)]
    [InlineData(100, "+100", 100)]
    public void CreateFeedback_WhenConnected_ShowsTheBrightnessWithTheBarCoveringTheWholeRange(double brightness, string text, double progress)
    {
        BrightnessAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Brightness, brightness)))
            .ShouldBe(new(text, progress));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "+1")]
    [InlineData(-1, "-1")]
    [InlineData(2.5, "+2.5")]
    [InlineData(-2.5, "-2.5")]
    [InlineData(0.30000000000000004, "+0.3")] // what three steps of 0.1 add up to
    [InlineData(0.04, "0")]
    [InlineData(-0.04, "0")] // rounds to zero, so no sign
    public void FormatBrightness_ShowsASignAndADecimalOnlyWhenThereIsOne(double brightness, string expected)
    {
        BrightnessAction
            .FormatBrightness(brightness)
            .ShouldBe(expected);
    }

    [Fact]
    public void FormatBrightness_UsesAFullStopWhateverTheLocale()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            BrightnessAction
                .FormatBrightness(-2.5)
                .ShouldBe("-2.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
