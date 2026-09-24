using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace DotnetApiTemplate.Api.Common.Json;

/// <summary>
/// The one JSON configuration of the API: request/response bodies, ProblemDetails and the OpenAPI
/// document all use it. Use <see cref="Options"/> for any manual <c>JsonSerializer</c> call.
/// </summary>
public static class JsonDefaults
{
    /// <summary>Maximum nesting depth of a JSON body (the framework default is 64).</summary>
    public const int MaxDepth = 32;

    /// <summary>Shared, read-only options with the API's settings, for manual (de)serialization.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Applies the API's settings on top of the web defaults (camelCase, case-insensitive reads).</summary>
    public static void Configure(JsonSerializerOptions options)
    {
        // Writing
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Keep non-ASCII text readable ("Café", not "Café") while still escaping
        // HTML-sensitive characters (<, >, &, ') so JSON can't be abused as HTML.
        options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

        // Reading: strict, so malformed or ambiguous input fails with a 400 instead of being guessed at.
        options.NumberHandling = JsonNumberHandling.Strict;   // "12" is not a number
        options.AllowDuplicateProperties = false;             // {"a":1,"a":2} is rejected
        options.ReadCommentHandling = JsonCommentHandling.Disallow;
        options.AllowTrailingCommas = false;
        options.MaxDepth = MaxDepth;
        // Unknown properties are ignored, so clients can send extra fields. Change to
        // JsonUnmappedMemberHandling.Disallow to reject them (catches client typos, less forgiving).
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;

        // Types
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));   // "Admin", never 1 or 99
        options.Converters.Add(new UtcDateTimeConverter());                                // UTC with "Z"
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Configure(options);
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
