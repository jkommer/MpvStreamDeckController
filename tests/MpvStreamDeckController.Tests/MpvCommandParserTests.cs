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
        { "show-text `!a b!`", [["show-text", "a b"]], null },
        { "show-text `!a \"b\" \\n c!`", [["show-text", "a \"b\" \\n c"]], null },
        { "show-text `!a;b#c!`; quit", [["show-text", "a;b#c"], ["quit"]], null },
    };

    [Theory]
    [InlineData("foo `")] // mpv checks the length from the command start, not the current position, so this opens a custom quote
    [InlineData("show-text `!abc")]
    [InlineData("show-text `!")]
    [InlineData("show-text \"abc")]
    [InlineData("show-text 'abc")]
    public void TryParse_UnterminatedQuote_ReturnsFalse(string text)
    {
        var isSuccessful = MpvCommandParser.TryParse(text, out var result, out var error);

        isSuccessful.ShouldBeFalse();
        result.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("Unterminated");
    }

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

    public static TheoryData<string, string> TryParse_EscapesInDoubleQuotes_AreResolvedData => new()
    {
        { @"show-text ""a\nb""", "a\nb" },
        { @"show-text ""a\tb\rc\bd\fe""", "a\tb\rc\bd\fe" },
        { @"show-text ""\""quoted\""""", "\"quoted\"" },
        { @"show-text ""back\\slash""", @"back\slash" },
        { @"show-text ""a\/b\'c""", "a/b'c" },
        { @"show-text ""\e[1m""", "\u001b[1m" },
        // \xHH appends one raw byte, so a multi-byte UTF-8 character is written as its bytes
        { @"show-text ""\x41\x4a\x4A""", "AJJ" },
        { @"show-text ""caf\xc3\xa9""", "café" },
        { @"show-text ""café €""", "café €" },
        // a high surrogate must be followed by a low one; together they make one code point
        { @"show-text ""😀""", "😀" },
        // like mpv, a lone low surrogate isn't rejected; it's encoded as invalid UTF-8, which decodes to U+FFFD
        { @"show-text ""\udc00""", "���" },
        // escapes are only resolved in double quotes
        { @"show-text 'a\nb'", @"a\nb" },
        { @"show-text a\nb", @"a\nb" },
    };

    [Theory]
    [MemberData(nameof(TryParse_EscapesInDoubleQuotes_AreResolvedData))]
    public void TryParse_EscapesInDoubleQuotes_AreResolved(string text, string expectedArgument)
    {
        MpvCommandParser.TryParse(text, out var result, out _).ShouldBeTrue();

        result.Commands.ShouldHaveSingleItem()[1].ShouldBe(expectedArgument);
    }

    [Theory]
    [InlineData(@"show-text ""\q""")]           // unknown escape
    [InlineData(@"show-text ""abc\")]           // backslash at the end
    [InlineData(@"show-text ""\x4""")]          // too short
    [InlineData(@"show-text ""\xZZ""")]         // not hex
    [InlineData(@"show-text ""\x+f""")]         // mpv's strtoll would accept this; deliberately rejected
    [InlineData(@"show-text ""\u12""")]         // too short
    [InlineData(@"show-text ""\uZZZZ""")]       // not hex
    [InlineData(@"show-text ""\ud83d""")]       // high surrogate on its own
    [InlineData(@"show-text ""\ud83dx\ude00""")] // high surrogate not directly followed by \u
    [InlineData(@"show-text ""\ud83dA""")] // high surrogate followed by something other than a low surrogate
    public void TryParse_BrokenEscape_ReturnsFalse(string text)
    {
        var isSuccessful = MpvCommandParser.TryParse(text, out var result, out var error);

        isSuccessful.ShouldBeFalse();
        result.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("Broken string escapes");
    }
}