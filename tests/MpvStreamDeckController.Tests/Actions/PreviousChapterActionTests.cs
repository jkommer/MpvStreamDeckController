using MpvStreamDeckController.Actions;
using MpvStreamDeckController.Actions.Settings;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class PreviousChapterActionTests
{
    private const string PreviousChapter = "[\"add\",\"chapter\",-1]";
    private const string PreviousFile = "[\"playlist-prev\"]";

    private static readonly ChapterSettings _withPlaylistNavigation = new(EnablePlaylistNavigation: true);
    private static readonly ChapterSettings _withoutPlaylistNavigation = new(EnablePlaylistNavigation: false);

    [Theory]
    [InlineData(1d, 3d)]
    [InlineData(2d, 3d)]
    public void CreateCommands_WhenThereIsAnEarlierChapter_GoesToThePreviousChapter(double chapter, double chapters)
    {
        PreviousChapterAction
            .CreateCommands(_withPlaylistNavigation, TestStates.Chapter(chapter, chapters))
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(PreviousChapter);
    }

    // chapter is zero-based, and -1 before the first chapter starts.
    [Theory]
    [InlineData(0d, 3d)] // on the first chapter
    [InlineData(-1d, 3d)] // before the first chapter
    [InlineData(null, 0d)] // a file without chapters
    [InlineData(null, null)] // nothing known yet, or nothing playing
    [InlineData(1d, null)]
    public void CreateCommands_WhenThereIsNoEarlierChapter_GoesToThePreviousFile(double? chapter, double? chapters)
    {
        PreviousChapterAction
            .CreateCommands(_withPlaylistNavigation, TestStates.Chapter(chapter, chapters))
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(PreviousFile);
    }

    [Fact]
    public void CreateCommands_WhenDisconnected_GoesToThePreviousFile()
    {
        PreviousChapterAction
            .CreateCommands(_withPlaylistNavigation, TestStates.Disconnected)
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(PreviousFile);
    }

    [Theory]
    [InlineData(2d, 3d)]
    [InlineData(0d, 3d)] // on the first chapter
    [InlineData(null, 0d)] // a file without chapters
    [InlineData(null, null)]
    public void CreateCommands_WithoutPlaylistNavigation_AlwaysGoesToThePreviousChapter(double? chapter, double? chapters)
    {
        PreviousChapterAction
            .CreateCommands(_withoutPlaylistNavigation, TestStates.Chapter(chapter, chapters))
            .ShouldHaveSingleItem().DecodedCommandJson()
            .ShouldBe(PreviousChapter);
    }
}
