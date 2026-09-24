using System.Text.Json;
using DotnetApiTemplate.Api.Common.Json;
using DotnetApiTemplate.Api.Domain.Enums;

namespace DotnetApiTemplate.UnitTests.Common.Json;

public sealed class JsonDefaultsTests
{
    private static readonly JsonSerializerOptions Options = JsonDefaults.Options;

    private sealed record Sample(string Name, int Count, UserRole Role, DateTime At, DateTime? Optional = null, string? Note = null);

    [Fact]
    public void Serialize_UsesCamelCaseEnumNamesAndOmitsNulls()
    {
        var json = JsonSerializer.Serialize(new Sample("x", 1, UserRole.Admin, DateTime.UnixEpoch), Options);

        Assert.Contains("\"name\":\"x\"", json);
        Assert.Contains("\"role\":\"Admin\"", json);
        Assert.DoesNotContain("note", json);
        Assert.DoesNotContain("optional", json);
    }

    [Fact]
    public void Serialize_KeepsUnicodeReadableButEscapesHtml()
    {
        var json = JsonSerializer.Serialize(new { name = "Café 日本 <script>" }, Options);

        Assert.Contains("Café 日本", json);
        Assert.DoesNotContain("<script>", json);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Serialize_WritesDatesAsUtcWithZ(DateTimeKind kind)
    {
        var at = new DateTime(2026, 9, 25, 10, 15, 30, 123, kind);

        var json = JsonSerializer.Serialize(new { at }, Options);

        Assert.Equal("{\"at\":\"2026-09-25T10:15:30.123Z\"}", json);
    }

    [Fact]
    public void Deserialize_ConvertsOffsetsToUtc()
    {
        var sample = JsonSerializer.Deserialize<Sample>(
            """{"name":"x","count":1,"role":"User","at":"2026-09-25T12:15:30+02:00"}""", Options)!;

        Assert.Equal(DateTimeKind.Utc, sample.At.Kind);
        Assert.Equal(new DateTime(2026, 9, 25, 10, 15, 30, DateTimeKind.Utc), sample.At);
    }

    [Fact]
    public void Deserialize_TreatsDatesWithoutOffsetAsUtc()
    {
        var sample = JsonSerializer.Deserialize<Sample>(
            """{"name":"x","count":1,"role":"User","at":"2026-09-25T10:15:30"}""", Options)!;

        Assert.Equal(new DateTime(2026, 9, 25, 10, 15, 30, DateTimeKind.Utc), sample.At);
    }

    [Theory]
    [InlineData("""{"name":"x","count":"12","role":"User","at":"2026-09-25T10:15:30Z"}""", "$.count")]   // number as string
    [InlineData("""{"name":"x","count":1,"role":1,"at":"2026-09-25T10:15:30Z"}""", "$.role")]            // enum as number
    [InlineData("""{"name":"x","count":1,"role":"User","at":"yesterday"}""", "$.at")]                     // not a date
    [InlineData("""{"name":"x","name":"y","count":1,"role":"User","at":"2026-09-25T10:15:30Z"}""", "$.name")]  // duplicate
    public void Deserialize_RejectsAmbiguousInput_WithPath(string json, string expectedPath)
    {
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Sample>(json, Options));

        Assert.Equal(expectedPath, exception.Path);
    }

    [Fact]
    public void Deserialize_RejectsCommentsAndTrailingCommas()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Sample>("""{"name":"x", /* c */ "count":1}""", Options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Sample>("""{"name":"x","count":1,}""", Options));
    }

    [Fact]
    public void Deserialize_RejectsTooDeepNesting()
    {
        var deep = string.Concat(Enumerable.Repeat("[", JsonDefaults.MaxDepth + 1)) + string.Concat(Enumerable.Repeat("]", JsonDefaults.MaxDepth + 1));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonElement>(deep, Options));
    }

    [Fact]
    public void Deserialize_IgnoresUnknownPropertiesAndIsCaseInsensitive()
    {
        var sample = JsonSerializer.Deserialize<Sample>(
            """{"NAME":"x","count":1,"role":"user","at":"2026-09-25T10:15:30Z","extra":true}""", Options)!;

        Assert.Equal("x", sample.Name);
        Assert.Equal(UserRole.User, sample.Role);
    }
}
