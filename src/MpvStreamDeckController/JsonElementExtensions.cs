using System.Text.Json;

namespace MpvStreamDeckController
{
    public static class JsonElementExtensions
    {
        public static string? GetPropertyText(this JsonElement element, string key)
            => element.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

        public static bool TryGetPropertyInt32(this JsonElement element, string key, out int value)
        {
            value = default;            
            return element.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value);
        }
    }
}