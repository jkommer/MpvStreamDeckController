using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Tests.Actions;

/// <summary>
/// Builds the Stream Deck payloads the action tests need. Their properties can only be set by deserialising, so they
/// are created from JSON, the way the SDK itself creates them.
/// </summary>
internal static class TestPayloads
{
    /// <summary>The payload an action is constructed with, holding its saved settings.</summary>
    public static InitialPayload Initial(JObject? settings = null)
        => new(Deserialize<AppearancePayload>(new() { ["settings"] = settings ?? [] }), null);

    /// <summary>The payload sent when the user changes an action's settings.</summary>
    public static ReceivedSettingsPayload Settings(JObject settings)
        => Deserialize<ReceivedSettingsPayload>(new() { ["settings"] = settings });

    /// <summary>The payload sent when a dial is turned.</summary>
    public static DialRotatePayload Rotate(int ticks)
        => Deserialize<DialRotatePayload>(new() { ["ticks"] = ticks });

    private static T Deserialize<T>(JObject json) => JsonConvert.DeserializeObject<T>(json.ToString())!;
}
