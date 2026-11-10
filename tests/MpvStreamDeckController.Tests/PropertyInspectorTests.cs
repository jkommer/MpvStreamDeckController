using System.Text.Json;
using MpvStreamDeckController.Actions.Settings;
using Shouldly;

namespace MpvStreamDeckController.Tests;

/// <summary>
/// Checks the property inspector files copied to the build output.
/// </summary>
public class PropertyInspectorTests
{
    private static readonly string PropertyInspectorFolder = Path.Combine(AppContext.BaseDirectory, "PropertyInspectors");

    [Fact]
    public void ScriptsJs_OsdOptions_MatchMpvOsdMode()
    {
        using var osdModes = JsonDocument.Parse(ReadMarkedJson("scripts.js", "osd-modes"));

        var values = osdModes.RootElement
            .EnumerateArray()
            .Select(mode => mode.GetProperty("value").GetString());

        values.ShouldBe(["", .. MpvOsdMode.All.Select(m => m.Prefix)]);
    }

    public static TheoryData<string> PropertyInspectors => [.. Directory.GetFiles(PropertyInspectorFolder, "*.html").Select(file => Path.GetFileName(file))];

    [Theory]
    [MemberData(nameof(PropertyInspectors))]
    public void PropertyInspectors_UsingSharedControls_LoadsScriptsJsAfterSdpiComponents(string propertyInspector)
    {
        var html = File.ReadAllText(Path.Combine(PropertyInspectorFolder, propertyInspector));

        if (!html.Contains("<mpv-"))
        {
            return;
        }

        var sdpi = html.IndexOf("<script src=\"sdpi-components.js\"></script>", StringComparison.Ordinal);
        var shared = html.IndexOf("<script src=\"scripts.js\"></script>", StringComparison.Ordinal);

        sdpi.ShouldBeGreaterThanOrEqualTo(0);
        shared.ShouldBeGreaterThan(sdpi);
    }

    /// <summary>
    /// The text between <c>/** name **/</c> and <c>/** /name **/</c> in a property inspector file.
    /// </summary>
    private static string ReadMarkedJson(string file, string name)
    {
        var text = File.ReadAllText(Path.Combine(PropertyInspectorFolder, file));
        var startMarker = $"/** {name} **/";
        var endMarker = $"/** /{name} **/";

        var start = text.IndexOf(startMarker, StringComparison.Ordinal);
        var end = text.IndexOf(endMarker, StringComparison.Ordinal);

        start.ShouldBeGreaterThanOrEqualTo(0, $"{file} should contain {startMarker}");
        end.ShouldBeGreaterThan(start, $"{file} should contain {endMarker} after {startMarker}");

        return text[(start + startMarker.Length)..end];
    }
}
