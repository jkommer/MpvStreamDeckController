using System.Globalization;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Actions.Settings;

/// <summary>
/// Reads individual values from an action's settings object, tolerating missing or wrongly typed values.
/// </summary>
internal static class SettingsValues
{
    public static string? ReadString(JObject settings, string key) => settings[key] is JValue { Value: string value } ? value : null;

    public static bool? ReadBool(JObject settings, string key) => settings[key] is JValue { Value: bool value } ? value : null;

    /// <summary>
    /// A positive number, or null if missing, blank or invalid. Accepts a JSON number, or text using '.' as the decimal separator whatever the system locale, so "1,5" is rejected rather than misread.
    /// </summary>
    public static double? ReadPositiveNumber(JObject settings, string key)
    {
        var token = settings[key];

        if (token is null or JValue { Type: JTokenType.Null } || (token is JValue { Value: string text } && string.IsNullOrWhiteSpace(text)))
        {
            return null;
        }

        double? number = token switch
        {
            JValue { Type: JTokenType.Integer or JTokenType.Float } value => value.Value<double>(),
            JValue { Value: string value } when double.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };

        if (number is > 0 and < double.PositiveInfinity)
        {
            return number;
        }

        LogHelper.Log(TracingLevel.WARN, $"Invalid value for setting '{key}': '{token}'; using the default.");
        return null;
    }
}
