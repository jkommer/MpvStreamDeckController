
using System.Diagnostics.CodeAnalysis;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// How precisely mpv seeks. Without one, mpv's hr-seek option decides. See the seek command here: https://mpv.io/manual/stable/#command-interface-seek-%3Ctarget%3E-[%3Cflags%3E]
/// </summary>
public sealed class MpvSeekType
{
    /// <summary>Lands exactly on the target, decoding from the previous keyframe. Slower on files with far-apart keyframes.</summary>
    public static readonly MpvSeekType Exact = new("exact");

    /// <summary>Lands on a keyframe near the target. Fast, but coarse on files with far-apart keyframes.</summary>
    public static readonly MpvSeekType Keyframes = new("keyframes");

    public static IReadOnlyList<MpvSeekType> All { get; } = [Exact, Keyframes];

    /// <summary>The flag added to the seek command, e.g. relative+exact.</summary>
    public string Flag { get; }

    private MpvSeekType(string flag) => Flag = flag;

    public static bool TryParse(string? flag, [NotNullWhen(true)] out MpvSeekType? type) => (type = All.FirstOrDefault(t => t.Flag == flag)) is not null;

    public override string ToString() => Flag;
}
