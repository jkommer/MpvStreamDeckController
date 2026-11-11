using System.Text.Json;

namespace MpvStreamDeckController;

public static class JsonElementExtensions
{
    extension(JsonElement element)
    {
        public string? GetPropertyText(string key)
            => element.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

        public bool TryGetPropertyInt64(string key, out long value)
        {
            value = default;
            return element.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out value);
        }
    }
}
