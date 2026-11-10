using System.Text.Json;

namespace MpvStreamDeckController.Tests;

internal static class MpvCommandTestExtensions
{
    /// <summary>
    /// The command's JSON array as mpv reads it: numbers exactly as written (so their formatting is checked too) and strings decoded,
    /// because the serializer escapes some characters (e.g. '+' as <c>+</c>), which mpv decodes.
    /// </summary>
    public static string DecodedCommandJson(this MpvCommand command)
    {
        using var json = JsonDocument.Parse(MpvProtocol.BuildCommand(command));

        var items = json.RootElement.GetProperty("command").EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? $"\"{item.GetString()}\"" : item.GetRawText());

        return $"[{string.Join(",", items)}]";
    }
}
