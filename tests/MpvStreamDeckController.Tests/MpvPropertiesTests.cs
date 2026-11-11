using System.Text.Json;
using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvPropertiesTests
{
    [Theory]
    [InlineData("""[{"name":"lavfi","label":"acompressor","enabled":true,"params":{"graph":"acompressor"}}]""", true)]
    [InlineData("""[{"name":"lavfi","label":"acompressor"}]""", true)] // no "enabled": treated as enabled
    [InlineData("""[{"name":"acompressor"}]""", true)] // no label: matched by name
    [InlineData("""[{"name":"lavfi","label":"loudnorm"},{"name":"lavfi","label":"acompressor"}]""", true)]
    [InlineData("""[1,"x",null,{"label":"acompressor"}]""", true)] // non-object items are skipped
    [InlineData("""[{"name":"lavfi","label":"acompressor","enabled":false}]""", false)]
    [InlineData("""[{"name":"acompressor","label":"other"}]""", false)] // the label wins over the name
    [InlineData("""[{"name":"lavfi","label":"loudnorm"}]""", false)]
    [InlineData("""[]""", false)]
    public void ACompressor_FromJson_ReportsWhetherTheFilterIsEnabled(string json, bool expected)
    {
        ReadACompressor(json).ShouldBe(expected);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""null""")]
    [InlineData("\"acompressor\"")]
    [InlineData("""1""")]
    public void ACompressor_FromJson_WhenNotAList_ReturnsNull(string json)
    {
        ReadACompressor(json).ShouldBeNull();
    }

    private static object? ReadACompressor(string json)
    {
        using var document = JsonDocument.Parse(json);

        return MpvProperties.ACompressor.FromJson
            .ShouldNotBeNull()
            .Invoke(document.RootElement);
    }
}
