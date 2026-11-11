using MpvStreamDeckController.Actions;
using MpvStreamDeckController.Actions.Settings;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class NextChapterActionTests
{
    private const string NextChapter = "[\"add\",\"chapter\",1]";
    private const string NextFile = "[\"playlist-next\"]";

    private static readonly ChapterSettings _withPlaylistNavigation = new(EnablePlaylistNavigation: true);
    private static readonly ChapterSettings _withoutPlaylistNavigation = new(EnablePlaylistNavigation: false);

    // chapter is zero-based, and -1 before the first chapter starts.
    [Theory]
    [InlineData(-1d, 3d)]
    [InlineData(0d, 3d)]
    [InlineData(1d, 3d)]
    public void CreateCommands_WhenThereIsALaterChapter_GoesToTheNextChapter(double chapter, double chapters)
    {
        NextChapterAction
            .CreateCommands(_withPlaylistNavigation, TestStates.Chapter(chapter, chapters))
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(NextChapter);
    }

    [Theory]
    [InlineData(2d, 3d)] // on the last chapter
    [InlineData(0d, 1d)] // on the only chapter
    [InlineData(null, 0d)] // a file without chapters
    [InlineData(null, null)] // nothing known yet, or nothing playing
    [InlineData(1d, null)]
    public void CreateCommands_WhenThereIsNoLaterChapter_GoesToTheNextFile(double? chapter, double? chapters)
    {
        NextChapterAction
            .CreateCommands(_withPlaylistNavigation, TestStates.Chapter(chapter, chapters))
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(NextFile);
    }

    [Fact]
    public void CreateCommands_WhenDisconnected_GoesToTheNextFile()
    {
        NextChapterAction
            .CreateCommands(_withPlaylistNavigation, TestStates.Disconnected)
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(NextFile);
    }

    [Theory]
    [InlineData(0d, 3d)]
    [InlineData(2d, 3d)] // on the last chapter
    [InlineData(null, 0d)] // a file without chapters
    [InlineData(null, null)]
    public void CreateCommands_WithoutPlaylistNavigation_AlwaysGoesToTheNextChapter(double? chapter, double? chapters)
    {
        NextChapterAction
            .CreateCommands(_withoutPlaylistNavigation, TestStates.Chapter(chapter, chapters))
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(NextChapter);
    }
}
