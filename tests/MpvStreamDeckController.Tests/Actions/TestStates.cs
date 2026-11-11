namespace MpvStreamDeckController.Tests.Actions;

/// <summary>Builds the mpv states the action tests feed to <c>CreateFeedback</c>.</summary>
internal static class TestStates
{
    public static MpvState Disconnected { get; } = new(1, false, new Dictionary<long, object>());

    /// <summary>A connected state holding the given property values.</summary>
    public static MpvState Connected(params (MpvProperty Property, object Value)[] values)
        => new(1, true, values.ToDictionary(v => v.Property.Id, v => v.Value));
}
