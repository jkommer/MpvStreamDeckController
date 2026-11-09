using System.Diagnostics.CodeAnalysis;

namespace MpvStreamDeckController;

/// <summary>
/// mpv's OSD command prefixes. See "Input Command Prefixes" here: https://mpv.io/manual/stable/#input-command-prefixes
/// </summary>
public sealed class MpvOsdMode
{
    public static readonly MpvOsdMode OsdAuto = new("osd-auto");
    public static readonly MpvOsdMode NoOsd = new("no-osd");
    public static readonly MpvOsdMode OsdBar = new("osd-bar");
    public static readonly MpvOsdMode OsdMsg = new("osd-msg");
    public static readonly MpvOsdMode OsdMsgBar = new("osd-msg-bar");

    public static IReadOnlyList<MpvOsdMode> All { get; } = [OsdAuto, NoOsd, OsdBar, OsdMsg, OsdMsgBar];

    public string Prefix { get; }

    private MpvOsdMode(string prefix) => Prefix = prefix;

    public static bool TryParse(string? prefix, [NotNullWhen(true)] out MpvOsdMode? mode) => (mode = All.FirstOrDefault(m => m.Prefix == prefix)) is not null;
    public override string ToString() => Prefix;
}
