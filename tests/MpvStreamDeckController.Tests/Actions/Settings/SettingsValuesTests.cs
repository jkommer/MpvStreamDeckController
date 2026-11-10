using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions.Settings;

public class SettingsValuesTests
{
    [Theory]
    [InlineData("{\"value\":\"2\"}", 2)]
    [InlineData("{\"value\":\"0.5\"}", 0.5)]
    [InlineData("{\"value\":\".25\"}", 0.25)]
    [InlineData("{\"value\":\" 10 \"}", 10)]
    [InlineData("{\"value\":5}", 5)]
    [InlineData("{\"value\":2.5}", 2.5)]
    public void ReadPositiveNumber_WhenValid_ReturnsIt(string json, double expected)
    {
        SettingsValues
            .ReadPositiveNumber(JObject.Parse(json), "value")
            .ShouldBe(expected);
    }

    [Theory]
    // not set
    [InlineData("{}")]
    [InlineData("{\"value\":\"\"}")]
    [InlineData("{\"value\":\"   \"}")]
    [InlineData("{\"value\":null}")]
    // a comma is never a decimal separator, whatever the locale
    [InlineData("{\"value\":\"0,5\"}")]
    [InlineData("{\"value\":\"1,000\"}")]
    // not positive
    [InlineData("{\"value\":\"0\"}")]
    [InlineData("{\"value\":\"-1\"}")]
    [InlineData("{\"value\":-1}")]
    [InlineData("{\"value\":0}")]
    // not a number
    [InlineData("{\"value\":\"abc\"}")]
    [InlineData("{\"value\":\"1e3\"}")]
    [InlineData("{\"value\":\"NaN\"}")]
    [InlineData("{\"value\":true}")]
    [InlineData("{\"value\":{}}")]
    public void ReadPositiveNumber_WhenMissingOrInvalid_ReturnsNull(string json)
    {
        SettingsValues
            .ReadPositiveNumber(JObject.Parse(json), "value")
            .ShouldBeNull();
    }

    [Theory]
    [InlineData("{\"value\":\"text\"}", "text")]
    [InlineData("{\"value\":\"\"}", "")]
    [InlineData("{\"value\":1}", null)]
    [InlineData("{\"value\":null}", null)]
    [InlineData("{}", null)]
    public void ReadString_ReturnsTheStringOrNull(string json, string? expected)
    {
        SettingsValues
            .ReadString(JObject.Parse(json), "value")
            .ShouldBe(expected);
    }

    [Theory]
    [InlineData("{\"value\":true}", true)]
    [InlineData("{\"value\":false}", false)]
    [InlineData("{\"value\":\"true\"}", null)]
    [InlineData("{\"value\":1}", null)]
    [InlineData("{\"value\":null}", null)]
    [InlineData("{}", null)]
    public void ReadBool_ReturnsTheBoolOrNull(string json, bool? expected)
    {
        SettingsValues
            .ReadBool(JObject.Parse(json), "value")
            .ShouldBe(expected);
    }
}
