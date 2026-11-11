using System.Globalization;
using MpvStreamDeckController.Actions;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class VolumeActionTests
{
    private const string VolumeIcon = "Images/dial/11a_volume_dial";
    private const string MutedIcon = "Images/dial/11b_volume_muted_dial";

    [Fact]
    public void CreateFeedback_WhenDisconnected_ShowsAnEmptyDialWithTheVolumeIcon()
    {
        VolumeAction
            .CreateFeedback(TestStates.Disconnected)
            .ShouldBe(new("", 0, VolumeIcon));
    }

    [Fact]
    public void CreateFeedback_WhenTheVolumeIsNotKnownYet_ShowsAnEmptyDialWithTheVolumeIcon()
    {
        VolumeAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Mute, true)))
            .ShouldBe(new("", 0, VolumeIcon));
    }

    [Theory]
    [InlineData(0, "0%", 0)]
    [InlineData(50, "50%", 50)]
    [InlineData(42.5, "42.5%", 42.5)]
    [InlineData(100, "100%", 100)]
    public void CreateFeedback_WithoutAVolumeMax_TreatsTheMaximumAs100(double volume, string text, double progress)
    {
        VolumeAction
            .CreateFeedback(TestStates.Connected((MpvProperties.Volume, volume)))
            .ShouldBe(new(text, progress, VolumeIcon));
    }

    [Theory]
    [InlineData(65, 130, 50)]
    [InlineData(130, 130, 100)]
    [InlineData(140, 130, 100)] // never past the end of the bar
    [InlineData(50, 0, 50)] // a volume-max of 0 is not usable, so 100 is assumed
    public void CreateFeedback_ScalesTheBarToVolumeMax(double volume, double max, double progress)
    {
        var feedback = VolumeAction.CreateFeedback(TestStates.Connected((MpvProperties.Volume, volume), (MpvProperties.VolumeMax, max)));

        feedback.Progress.ShouldBe(progress, 0.001);
    }

    [Fact]
    public void CreateFeedback_WhenMuted_ShowsMutedWithTheMutedIconAndKeepsTheBar()
    {
        var feedback = VolumeAction.CreateFeedback(TestStates.Connected((MpvProperties.Volume, 40d), (MpvProperties.Mute, true)));

        feedback.ShouldBe(new("Muted", 40, MutedIcon));
    }

    [Fact]
    public void CreateFeedback_WhenNotMuted_ShowsTheVolumeIcon()
    {
        var feedback = VolumeAction.CreateFeedback(TestStates.Connected((MpvProperties.Volume, 40d), (MpvProperties.Mute, false)));

        feedback.ShouldBe(new("40%", 40, VolumeIcon));
    }

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
