using System.Text.Json;

namespace MpvStreamDeckController;

/// <summary>
/// helpers for mpv's JSON IPC protocol.
/// </summary>
public static class MpvProtocol
{
    public static string BuildCommand(MpvCommand command, int? requestId = null)
    {
        var components = WithInputPrefixes(command);

        return requestId is { } id
            ? JsonSerializer.Serialize(new { command = components, request_id = id }) + "\n"
            : JsonSerializer.Serialize(new { command = components }) + "\n";
    }

    /// <summary>
    /// Apply input prefixes, if any: see https://mpv.io/manual/stable/#input-command-prefixes
    /// </summary>
    private static object[] WithInputPrefixes(MpvCommand command)
    {
        var prefixes = new List<object>(2);

        if (command.Osd is { } osd)
        {
            prefixes.Add(osd.Prefix);
        }

        if (command.ExpandProperties)
        {
            prefixes.Add("expand-properties");
        }

        return prefixes.Count == 0 ? command.Components : [.. prefixes, .. command.Components];
    }

    public static string BuildObserveProperty(int id, string property)
        => BuildCommand(new("observe_property", id, property));

    public static string BuildUnobserveProperty(int id)
        => BuildCommand(new("unobserve_property", id));

    /// <summary>
    /// Parses a <c>property-change</c> event line; false for anything else (replies, other events, garbage).
    /// <paramref name="value"/> is a double, bool, string, or null when the property is unavailable.
    /// </summary>
    public static bool TryParsePropertyChange(string? line, out int id, out object? value)
    {
        id = 0;
        value = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object || root.GetPropertyText("event") != "property-change" || !root.TryGetPropertyInt32("id", out id))
            {
                return false;
            }

            if (root.TryGetProperty("data", out var data))
            {
                value = ToValue(data);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static object? ToValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array or JsonValueKind.Object => element.Clone(),
            _ => null,
        };
    }
}