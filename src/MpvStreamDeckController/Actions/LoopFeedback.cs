namespace MpvStreamDeckController.Actions;

/// <summary>What the loop keys show, shared by Toggle Loop File and Toggle Loop Playlist.</summary>
internal static class LoopFeedback
{
    /// <summary>
    /// The on image (state 1) if <paramref name="loopProperty"/> says mpv is looping, otherwise the off image (state 0).
    /// <para>
    /// mpv reports loop-file and loop-playlist as false for "no", as text for "inf" (and "force"), and as a number for a
    /// fixed count, so any text or number means looping.
    /// </para>
    /// </summary>
    public static KeyFeedback Create(MpvState state, MpvProperty loopProperty)
    {
        var looping = state.Text(loopProperty) is not null || state.Number(loopProperty) is not null;

        return new(looping ? (uint)1 : 0, null);
    }
}
