using System.Text.Json;

namespace MpvStreamDeckController
{
    public static class MpvProtocol
    {
        public static string BuildCommand(object[] command, int? requestId = null)
            => requestId is { } id
                ? JsonSerializer.Serialize(new { command, request_id = id }) + "\n"
                : JsonSerializer.Serialize(new { command }) + "\n";
    }
}
