using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvCommandParserTests
{
    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \n")]
    public void TryParse_NullOrWhitespace_ReturnsFalse(string? text)
    {
        var isSuccessful = MpvCommandParser.TryParse(text, out var command, out var error);
        
        isSuccessful.ShouldBeFalse();
        command.ShouldBeNull();
        error.ShouldNotBeNull();
    }

    public static TheoryData<string, string[][], string?> TryParse_ValidMpvCommands_AreCorrectlyParsedData => new()
    {
        { "seek 10; pause", [["seek", "10"], ["pause"]], null },
        { "seek 10 # jump ahead", [["seek", "10"]], "jump ahead" },
        { "show-text \"a b\"; quit # bye", [["show-text", "a b"], ["quit"]],"bye" },
        { "cycle pause", [["cycle", "pause"]], null },
        { "seek 10 relative", [["seek", "10", "relative"]], null },
        { "add volume -5", [["add", "volume", "-5"]], null },
        { "  cycle   pause  ", [["cycle", "pause"]], null },
        { "cycle\tpause", [["cycle", "pause"]], null },
        { "af toggle lavfi=[acompressor=ratio=3]", [["af", "toggle", "lavfi=[acompressor=ratio=3]"]], null },
        { "script-message my-script \"hello world\"", [["script-message", "my-script", "hello world"]], null },
        { "cycle \"\"", [["cycle", ""]], null },
        { @"loadfile ""C:\\Videos\\my movie.mkv""", [["loadfile", @"C:\Videos\my movie.mkv"]], null },
        { @"loadfile 'C:\Videos\my movie.mkv'", [["loadfile", @"C:\Videos\my movie.mkv"]], null },
        { @"a""b c""d", [[@"a""b", @"c""d"]], null },
    };

    [Theory]
    [MemberData(nameof(TryParse_ValidMpvCommands_AreCorrectlyParsedData))]
    public void TryParse_ValidMpvCommands_AreCorrectlyParsed(string text, string[][] expectedCommands, string? expectedDescription)
    {
        var isSuccessful = MpvCommandParser.TryParse(text, out var result, out var error);

        isSuccessful.ShouldBeTrue();
        error.ShouldBeNull();

        result.ShouldNotBeNull();
        result.Commands.ShouldBe(expectedCommands);
        result.Description.ShouldBe(expectedDescription);        
    }
}