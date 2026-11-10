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
        MpvProtocol
            .BuildCommand(commands, requestId)
            .ShouldBe(expectedOutput);
    }

    [Theory]
    [InlineData(false, null, "{\"command\":[\"show-text\",\"${time-pos}\"]}\n")]
    [InlineData(true, null, "{\"command\":[\"expand-properties\",\"show-text\",\"${time-pos}\"]}\n")]
    [InlineData(false, "osd-auto", "{\"command\":[\"osd-auto\",\"show-text\",\"${time-pos}\"]}\n")]
    [InlineData(false, "no-osd", "{\"command\":[\"no-osd\",\"show-text\",\"${time-pos}\"]}\n")]
    [InlineData(false, "osd-bar", "{\"command\":[\"osd-bar\",\"show-text\",\"${time-pos}\"]}\n")]
    [InlineData(false, "osd-msg", "{\"command\":[\"osd-msg\",\"show-text\",\"${time-pos}\"]}\n")]
    [InlineData(true, "osd-msg-bar", "{\"command\":[\"osd-msg-bar\",\"expand-properties\",\"show-text\",\"${time-pos}\"]}\n")]
    public void BuildCommand_WithPrefixOptions_PrependsPrefixes(bool expandProperties, string? osdPrefix, string expectedOutput)
    {
        var osd = osdPrefix is null ? null : MpvOsdMode.All.Single(mode => mode.Prefix == osdPrefix);

        var command = new MpvCommand("show-text", "${time-pos}") { ExpandProperties = expandProperties, Osd = osd };

        MpvProtocol
            .BuildCommand(command)
            .ShouldBe(expectedOutput);
    }

    [Fact]
    public void BuildCommand_WithPrefixOptions_KeepsUserPrefixesAfterOurs()
    {
        MpvCommand command = new object[] { "raw", "show-text", "${time-pos}" };
        command = command with { ExpandProperties = true };

        MpvProtocol
            .BuildCommand(command)
            .ShouldBe("{\"command\":[\"expand-properties\",\"raw\",\"show-text\",\"${time-pos}\"]}\n");
    }

    [Fact]
    public void BuildObserveProperty_WithProperty_ReturnsCorrectlyFormattedObserveCommand()
    {
        foreach (var property in MpvProperties.All.Values)
        {
            MpvProtocol
                .BuildObserveProperty(property.Id, property.Name)
                .ShouldBe($"{{\"command\":[\"observe_property\",{ property.Id },\"{ property.Name }\"]}}\n");
        }
    }

    [Fact]
    public void BuildUnobserveProperty_WithProperty_ReturnsCorrectlyFormattedObserveCommand()
    {
        foreach (var property in MpvProperties.All.Values)
        {
            MpvProtocol
                .BuildUnobserveProperty(property.Id)
                .ShouldBe($"{{\"command\":[\"unobserve_property\",{ property.Id }]}}\n");
        }
    }

    private static MpvPropertyChange ParsePropertyChange(string line)
        => MpvProtocol.ParseMessage(line).ShouldBeOfType<MpvPropertyChange>();

    [Theory]
    [InlineData("{\"event\":\"property-change\",\"id\":1,\"data\":5}", 1, 5.0)]
    [InlineData("{\"event\":\"property-change\",\"id\":2,\"data\":-0.25}", 2, -0.25)]
    [InlineData("{\"event\":\"property-change\",\"id\":3,\"name\":\"time-pos\", \"data\": 12.5}", 3, 12.5)]
    [InlineData("{\"event\":\"property-change\",\"id\":4,\"data\":0}", 4, 0.0)]
    public void ParseMessage_WhenPropertyChangeDataIsANumber_ReturnsItAsDouble(string line, int expectedId, double expectedValue)
    {
        var change = ParsePropertyChange(line);

        change.PropertyId.ShouldBe(expectedId);

        change.Value
            .ShouldBeOfType<double>()
            .ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ParseMessage_WhenPropertyChangeDataIsABoolean_ReturnsBool(string json, bool expected)
    {
        var change = ParsePropertyChange($"{{\"event\":\"property-change\",\"id\":1,\"name\":\"pause\",\"data\":{json}}}");

        change.PropertyId.ShouldBe(1);

        change.Value
            .ShouldBeOfType<bool>()
            .ShouldBe(expected);
    }

    [Fact]
    public void ParseMessage_WhenPropertyChangeDataIsAString_ReturnsString()
    {
        ParsePropertyChange("{\"event\":\"property-change\",\"id\":1,\"name\":\"mute\",\"data\":\"mute\"}").Value
            .ShouldBeOfType<string>().ShouldBe("mute");
    }

    [Theory]
    [InlineData("{\"event\":\"property-change\",\"id\":5,\"name\":\"mute\",\"data\":null}")]
    [InlineData("{\"event\":\"property-change\",\"id\":5,\"name\":\"time-pos\"}")]
    public void ParseMessage_WhenPropertyChangeDataIsNullOrMissing_ReturnsNullValue(string line)
    {
        var change = ParsePropertyChange(line);

        change.PropertyId.ShouldBe(5);
        change.Value.ShouldBeNull();
    }

    [Fact]
    public void ParseMessage_WhenPropertyChangeDataIsAnArray_ReturnsJsonElement()
    {
        var change = ParsePropertyChange("{\"event\":\"property-change\",\"id\":9,\"data\":[{\"name\":\"mute\",\"enabled\":true}]}");

        change.PropertyId.ShouldBe(9);

        var element = change.Value.ShouldBeOfType<JsonElement>();
        element.ValueKind.ShouldBe(JsonValueKind.Array);
        element
            .GetArrayLength()
            .ShouldBe(1);

        element[0]
            .GetProperty("name")
            .GetString()
            .ShouldBe("mute");

        element[0]
            .GetProperty("enabled")
            .GetBoolean()
            .ShouldBeTrue();
    }

    [Fact]
    public void ParseMessage_WhenPropertyChangeDataIsAnObject_ReturnsJsonElement()
    {
        var element = ParsePropertyChange("{\"event\":\"property-change\",\"id\":9,\"name\":\"settings\",\"data\":{\"item\": 1}}").Value
            .ShouldBeOfType<JsonElement>();

        element.ValueKind.ShouldBe(JsonValueKind.Object);

        element
            .GetProperty("item")
            .GetInt32()
            .ShouldBe(1);
    }

    [Fact]
    public void ParseMessage_WhenPropertiesAreInADifferentOrderOrExtraOnesExist_StillParses()
    {
        var change = ParsePropertyChange("{\"data\":42,\"extra\":[1,2,3],\"id\":8,\"name\":\"volume\",\"event\":\"property-change\"}");

        change.PropertyId.ShouldBe(8);

        change.Value
            .ShouldBeOfType<double>()
            .ShouldBe(42.0);
    }


    [Theory]
    [InlineData("{\"request_id\":7,\"error\":\"success\",\"data\":null}", 7L, "success", true)]
    [InlineData("{\"data\":null,\"request_id\":12,\"error\":\"error running command\"}", 12L, "error running command", false)]
    [InlineData("{\"error\":\"property unavailable\",\"request_id\":0}", 0L, "property unavailable", false)]
    [InlineData("{\"request_id\":9007199254740993,\"error\":\"success\"}", 9007199254740993L, "success", true)]
    public void ParseMessage_WhenLineIsAReply_ReturnsTheReply(string line, long expectedId, string expectedError, bool expectedSuccess)
    {
        var reply = MpvProtocol
            .ParseMessage(line)
            .ShouldBeOfType<MpvReply>();

        reply.RequestId.ShouldBe(expectedId);
        reply.Error.ShouldBe(expectedError);
        reply.IsSuccess.ShouldBe(expectedSuccess);
    }

    [Theory]
    // empty or invalid JSON
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("not json")]
    [InlineData("{\"event\": \"property-change\", \"id\": 1")]
    // valid JSON, but not an object
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("\"property-change\"")]
    [InlineData("null")]
    [InlineData("{}")]
    // events we don't handle, or malformed property changes
    [InlineData("{\"event\":\"seek\"}")]
    [InlineData("{\"event\":\"seek\",\"id\": 7,\"data\":1}")]
    [InlineData("{\"event\": 123, \"id\": 1, \"data\": 5}")]
    [InlineData("{\"event\": \"property-change\", \"data\": 5}")]
    [InlineData("{\"event\": \"property-change\", \"id\": \"abc\", \"data\": 5}")]
    [InlineData("{\"id\": 1, \"data\": 5}")]
    // events are never replies, even with reply-like fields; malformed replies
    [InlineData("{\"event\":\"end-file\",\"reason\":\"error\",\"error\":\"loading failed\",\"request_id\":1}")]
    [InlineData("{\"request_id\":1}")]
    [InlineData("{\"error\":\"success\"}")]
    [InlineData("{\"request_id\":\"1\",\"error\":\"success\"}")]
    [InlineData("{\"request_id\":1.5,\"error\":\"success\"}")]
    [InlineData("{\"request_id\":1,\"error\":5}")]
    public void ParseMessage_WhenLineIsNeitherAReplyNorAPropertyChange_ReturnsNull(string? line)
    {
        MpvProtocol
            .ParseMessage(line)
            .ShouldBeNull();
    }
}
