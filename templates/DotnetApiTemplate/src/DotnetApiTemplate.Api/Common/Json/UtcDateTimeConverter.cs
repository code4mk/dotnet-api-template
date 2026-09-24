using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetApiTemplate.Api.Common.Json;

/// <summary>
/// Writes every <see cref="DateTime"/> as ISO 8601 UTC with a "Z" (2026-09-25T10:15:30.123Z) and reads
/// incoming values as UTC. Values without a kind are treated as UTC, which is how the API stores them.
/// Incoming values with an offset (+02:00) are converted to UTC. Also used for <c>DateTime?</c>.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TryGetDateTime keeps "no offset" as Unspecified (treated as UTC below) instead of assuming the
        // server's local time zone, and converts values with an offset to Local (converted back to UTC).
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var value))
        {
            throw new JsonException("Expected an ISO 8601 date and time, e.g. 2026-09-25T10:15:30Z.");
        }

        return ToUtc(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToUtc(value).ToString(Format, CultureInfo.InvariantCulture));

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
