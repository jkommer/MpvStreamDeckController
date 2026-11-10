using System.Text.Json;

namespace MpvStreamDeckController;

/// <summary>
/// helpers for mpv's JSON IPC protocol.
/// </summary>
public static class MpvProtocol
{
    public static string BuildCommand(MpvCommand command, long? requestId = null)
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

    public static string BuildObserveProperty(long id, string property)
        => BuildCommand(new("observe_property", id, property));

    public static string BuildUnobserveProperty(long id)
        => BuildCommand(new("unobserve_property", id));

    /// <summary>
    /// Parses one line received from mpv: a property-change event, a command reply, or null for anything else.
    /// </summary>
    public static MpvMessage? ParseMessage(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("event", out _))
            {
                if (root.GetPropertyText("event") != "property-change" || !root.TryGetPropertyInt64("id", out var propertyId))
                {
                    return null;
                }

                return new MpvPropertyChange(propertyId, root.TryGetProperty("data", out var data) ? ToValue(data) : null);
            }

            // Replies have no "event"; mpv always includes "error" and copies "request_id" back (0 if none was sent).
            if (root.TryGetPropertyInt64("request_id", out var requestId) && root.GetPropertyText("error") is { } error)
            {
                return new MpvReply(requestId, error);
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
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