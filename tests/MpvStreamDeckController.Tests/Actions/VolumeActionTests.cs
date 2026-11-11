using System.Globalization;
using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class VolumeActionTests
{
    [Theory]
    [InlineData(42, "42%")]
    [InlineData(42.5, "42.5%")]
    [InlineData(0.5, "0.5%")]
    [InlineData(0, "0%")]
    [InlineData(130, "130%")]
    public void FormatVolume_ShowsADecimalOnlyWhenThereIsOne(double volume, string expected)
    {
        VolumeAction
            .FormatVolume(volume)
            .ShouldBe(expected);
    }

    [Fact]
    public void FormatVolume_UsesAFullStopWhateverTheLocale()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            VolumeAction
                .FormatVolume(42.5)
                .ShouldBe("42.5%");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
