using Shouldly;
using System.Text.Json;

namespace MpvStreamDeckController.Tests;

public class MpvProtocolTests
{
    [Theory]
    [InlineData(new object[] { "cycle", "audio" }, null, "{\"command\":[\"cycle\",\"audio\"]}\n")]
    [InlineData(new object[] { "cycle", "sub" }, null, "{\"command\":[\"cycle\",\"sub\"]}\n")]
    [InlineData(new object[] { "cycle", "pause" }, 1, "{\"command\":[\"cycle\",\"pause\"],\"request_id\":1}\n")]
    [InlineData(new object[] { "playlist-next" }, null, "{\"command\":[\"playlist-next\"]}\n")]
    [InlineData(new object[] { "playlist-prev" }, 2, "{\"command\":[\"playlist-prev\"],\"request_id\":2}\n")]
    [InlineData(new object[] { "add", "chapter", 1 }, null, "{\"command\":[\"add\",\"chapter\",1]}\n")]
    [InlineData(new object[] { "add", "chapter", -1 }, 3, "{\"command\":[\"add\",\"chapter\",-1],\"request_id\":3}\n")]
    [InlineData(new object[] { "seek", 5, "relative" }, null, "{\"command\":[\"seek\",5,\"relative\"]}\n")]
    [InlineData(new object[] { "add", "volume", 10 }, 4, "{\"command\":[\"add\",\"volume\",10],\"request_id\":4}\n")]
    [InlineData(new object[] { "script-message", "toggle-acompressor" }, null, "{\"command\":[\"script-message\",\"toggle-acompressor\"]}\n")]
    [InlineData(new object[] { "script-message", "toggle-acompressor" }, 5, "{\"command\":[\"script-message\",\"toggle-acompressor\"],\"request_id\":5}\n")]
    public void BuildCommand_WithValidInput_ReturnsCorrectlyFormattedCommands(object[] commands, int? requestId, string expectedOutput)
    {
        MpvProtocol.BuildCommand(commands, requestId).ShouldBe(expectedOutput);
    }

    [Fact]
    public void BuildObserveProperty_WithProperty_ReturnsCorrectlyFormattedObserveCommand()
    {
        foreach (var property in MpvProperties.All.Values)
        {
            MpvProtocol.BuildObserveProperty(property.Id, property.Name).ShouldBe($"{{\"command\":[\"observe_property\",{ property.Id },\"{ property.Name }\"]}}\n");
        }
    }

    [Fact]
    public void BuildUnobserveProperty_WithProperty_ReturnsCorrectlyFormattedObserveCommand()
    {
        foreach (var property in MpvProperties.All.Values)
        {
            MpvProtocol.BuildUnobserveProperty(property.Id).ShouldBe($"{{\"command\":[\"unobserve_property\",{ property.Id }]}}\n");
        }
    }

    public static TheoryData<string?> TryParsePropertyChange_WhenLineIsEmptyOrInvalidJson_ShouldReturnFalseData => new()
    {
        { (string?) null },
        { "" },
        { "   " },
        { "{" },
        { "not json" },
        { "{\"event\": \"property-change\", \"id\": 1" },
    };

    [Theory]
    [MemberData(nameof(TryParsePropertyChange_WhenLineIsEmptyOrInvalidJson_ShouldReturnFalseData))]
    public void TryParsePropertyChange_WhenLineIsEmptyOrInvalidJson_ShouldReturnFalse(string? line)
    {
        var result = MpvProtocol.TryParsePropertyChange(line, out var id, out var value);

        result.ShouldBeFalse();
        id.ShouldBe(0);
        value.ShouldBeNull();
    }

    [Fact]
    public void TryParsePropertyChange_WhenEventIsWrongButIdIsPresent_ShouldNotLeakTheId()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"event\":\"seek\",\"id\": 7,\"data\":1}", out var id, out _);

        result.ShouldBeFalse();
        id.ShouldBe(0);
    }

    [Theory]
    [InlineData("{\"event\":\"property-change\",\"id\":1,\"data\":5}", 1, 5.0)]
    [InlineData("{\"event\":\"property-change\",\"id\":2,\"data\":-0.25}", 2, -0.25)]
    [InlineData("{\"event\":\"property-change\",\"id\":3,\"name\":\"time-pos\", \"data\": 12.5}", 3, 12.5)]
    [InlineData("{\"event\":\"property-change\",\"id\":4,\"data\":0}", 4, 0.0)]
    public void TryParsePropertyChange_WhenDataIsANumber_ShouldReturnItAsDouble(string line, int expectedId, double expectedValue)
    {
        var result = MpvProtocol.TryParsePropertyChange(line, out var id, out var value);

        result.ShouldBeTrue();
        id.ShouldBe(expectedId);
        value.ShouldBeOfType<double>().ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void TryParsePropertyChange_WhenDataIsABoolean_ShouldReturnBool(string json, bool expected)
    {
        var result = MpvProtocol.TryParsePropertyChange($"{{\"event\":\"property-change\",\"id\":1,\"name\":\"time-pos\",\"data\":{json}}}", out var id, out var value);

        result.ShouldBeTrue();
        id.ShouldBe(1);
        value.ShouldBeOfType<bool>().ShouldBe(expected);
    }


    [Fact]
    public void TryParsePropertyChange_WhenDataIsAString_ShouldReturnString()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"event\":\"property-change\",\"id\":1,\"name\":\"mute\",\"data\":\"mute\"}", out _, out var value);

        result.ShouldBeTrue();
        value.ShouldBeOfType<string>().ShouldBe("mute");
    }

    [Fact]
    public void TryParsePropertyChange_WhenDataIsExplicitNull_ShouldReturnTrueWithNullValue()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"event\":\"property-change\",\"id\":5,\"name\":\"mute\",\"data\":null}", out var id, out var value);

        result.ShouldBeTrue();
        id.ShouldBe(5);
        value.ShouldBeNull();
    }

    [Fact]
    public void TryParsePropertyChange_WhenDataIsMissing_ShouldReturnTrueWithNullValue()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"event\":\"property-change\",\"id\":5,\"name\":\"time-pos\"}", out var id, out var value);

        result.ShouldBeTrue();
        id.ShouldBe(5);
        value.ShouldBeNull();
    }

    [Fact]
    public void TryParsePropertyChange_WhenDataIsAnArray_ShouldReturnJsonElement()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"event\":\"property-change\",\"id\":9,\"data\":[{\"name\":\"mute\",\"enabled\":true}]}", out var id, out var value);

        result.ShouldBeTrue();
        id.ShouldBe(9);

        var element = value.ShouldBeOfType<JsonElement>();
        element.ValueKind.ShouldBe(JsonValueKind.Array);
        element.GetArrayLength().ShouldBe(1);
        element[0].GetProperty("name").GetString().ShouldBe("mute");
        element[0].GetProperty("enabled").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public void TryParsePropertyChange_WhenDataIsAnObject_ShouldReturnJsonElement()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"event\":\"property-change\",\"id\":9,\"name\":\"settings\",\"data\":{\"item\": 1}}", out _, out var value);

        result.ShouldBeTrue();

        var element = value.ShouldBeOfType<JsonElement>();
        element.ValueKind.ShouldBe(JsonValueKind.Object);
        element.GetProperty("item").GetInt32().ShouldBe(1);
    }

    [Fact]
    public void TryParsePropertyChange_WhenPropertiesAreInADifferentOrderOrExtraOnesExist_ShouldStillParse()
    {
        var result = MpvProtocol.TryParsePropertyChange("{\"data\":42,\"extra\":[1,2,3],\"id\":8,\"name\":\"volume\",\"event\":\"property-change\"}", out var id, out var value);

        result.ShouldBeTrue();
        id.ShouldBe(8);
        value.ShouldBeOfType<double>().ShouldBe(42.0);
    }

    public static TheoryData<string> TryParsePropertyChange_WhenLineIsNotAValidPropertyChange_ShouldReturnFalseData => new()
    {
        { "[]" },
        { "42" },
        { "\"property-change\"" },
        { "null" },
        { "{}" },
        { "{\"id\": 1, \"data\": 5}" },
        { "{\"event\": \"seek\", \"id\": 1, \"data\": 5}" },
        { "{\"event\": 123, \"id\": 1, \"data\": 5}" },
        { "{\"event\": \"property-change\", \"data\": 5}" },
        { "{\"event\": \"property-change\", \"id\": \"abc\", \"data\": 5}" },
    };

    [Theory]
    [MemberData(nameof(TryParsePropertyChange_WhenLineIsNotAValidPropertyChange_ShouldReturnFalseData))]
    public void TryParsePropertyChange_WhenLineIsNotAValidPropertyChange_ShouldReturnFalse(string line)
    {
        var result = MpvProtocol.TryParsePropertyChange(line, out var id, out var value);

        result.ShouldBeFalse();
        id.ShouldBe(0);
        value.ShouldBeNull();
    }
}
