using System.Text.Json;

namespace MpvStreamDeckController;

/// <summary>
/// helpers for mpv's JSON IPC protocol.
/// </summary>
public static class MpvProtocol
{
    public static string BuildCommand(MpvCommand command, int? requestId = null)
        => requestId is { } id
            ? JsonSerializer.Serialize(new { command = command.Components, request_id = id }) + "\n"
            : JsonSerializer.Serialize(new { command = command.Components }) + "\n";

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