using MpvStreamDeckController.Actions.Settings;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class MpvOsdModeTests
{
    [Fact]
    public void TryParse_WithEachModesPrefix_RoundTrips()
    {
        foreach (var mode in MpvOsdMode.All)
        {
            MpvOsdMode
                .TryParse(mode.Prefix, out var parsed)
                .ShouldBeTrue();

            parsed.ShouldBeSameAs(mode);
        }
    }

    [Fact]
    public void All_HasUniquePrefixes()
    {
        MpvOsdMode.All
            .Select(mode => mode.Prefix)
            .ShouldBeUnique();
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("osd")]
    [InlineData("raw")]
    public void TryParse_WithUnknownPrefix_ReturnsFalse(string? prefix)
    {
        MpvOsdMode
            .TryParse(prefix, out var mode)
            .ShouldBeFalse();

        mode.ShouldBeNull();
    }
}
