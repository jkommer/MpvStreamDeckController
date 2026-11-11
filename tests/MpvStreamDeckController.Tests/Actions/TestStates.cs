namespace MpvStreamDeckController.Tests.Actions;

/// <summary>Builds the mpv states the action tests feed to <c>CreateFeedback</c>.</summary>
internal static class TestStates
{
    public static MpvState Disconnected { get; } = new(1, false, new Dictionary<long, object>());

    /// <summary>A connected state holding the given property values.</summary>
    public static MpvState Connected(params (MpvProperty Property, object Value)[] values) => Connected(1, values);

    /// <summary>A connected state with a specific version, for tests that raise several states in a row.</summary>
    public static MpvState Connected(long version, params (MpvProperty Property, object Value)[] values)
        => new(version, true, values.ToDictionary(v => v.Property.Id, v => v.Value));

    /// <summary>
    /// A connected state with mpv's <c>chapter</c> (zero-based index of the current chapter) and <c>chapters</c> (how
    /// many the file has). Null leaves that property out, as when mpv has no value for it.
    /// </summary>
    public static MpvState Chapter(double? chapter, double? chapters)
    {
        var values = new List<(MpvProperty Property, object Value)>();

        if (chapter is { } current)
        {
            values.Add((MpvProperties.Chapter, current));
        }

        if (chapters is { } count)
        {
            values.Add((MpvProperties.Chapters, count));
        }

        return Connected([.. values]);
    }
}
