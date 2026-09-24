# JSON serialization

One configuration, `Common/Json/JsonDefaults.cs`, is used for request bodies, responses, ProblemDetails and
the OpenAPI document. It's applied in `AddApiDefaults` (`ConfigureHttpJsonOptions`), so endpoints need
nothing extra.

## Responses

| Rule | Example |
| --- | --- |
| camelCase names | `FullName` → `"fullName"` |
| Null properties are omitted | `UpdatedAt = null` → not in the JSON |
| Enums by name | `"role": "Admin"` |
| Dates in ISO 8601 UTC with `Z` | `"createdAt": "2026-09-25T10:15:30.123Z"` |
| Readable Unicode | `"José Müller"`, not `"José Müller"` |
| HTML-sensitive characters escaped | `<script>` → `<script>` |

## Requests

Names are case-insensitive (`"FullName"` and `"fullname"` both work) and unknown properties are ignored.
Everything else is strict; these fail with `400 invalid_json` and say where (`at '$.price' (line 1)`):

| Rejected | Send instead |
| --- | --- |
| Number as a string: `"price": "49.99"` | `"price": 49.99` |
| Enum as a number: `"role": 1` | `"role": "Admin"` |
| Date that isn't ISO 8601: `"25/09/2026"` | `"2026-09-25T10:15:30Z"` |
| Duplicate property: `{"name": "A", "name": "B"}` | one `name` |
| Comments, trailing commas | plain JSON |
| Nesting deeper than 32 levels | flatter data |

Dates with an offset (`2026-09-25T12:15:30+02:00`) are converted to UTC; dates without an offset are
read as UTC, never as the server's local time.

Missing or empty required fields are **not** JSON errors: validation reports them per field
(`400` with `errors`), which is more useful to clients.

## Serializing by hand

Use the shared options so manual JSON matches the API:

```csharp
using DotnetApiTemplate.Api.Common.Json;

var json = JsonSerializer.Serialize(value, JsonDefaults.Options);
var model = JsonSerializer.Deserialize<MyType>(json, JsonDefaults.Options);
```

`JsonDefaults.Options` is read-only. For calls to **external** APIs, use their conventions instead
(many use snake_case or send numbers as strings): create separate `JsonSerializerOptions` for that client.

## Dates in your code

Store and compute times in UTC: `DateTime.UtcNow`, or better an injected `TimeProvider`
(`timeProvider.GetUtcNow()`), which tests can control. `CreatedAt`/`UpdatedAt` are set in UTC by
`AppDbContext`. Convert to a user's time zone only in the client.

## Changing the rules

All in `JsonDefaults.Configure`:

| Want | Change |
| --- | --- |
| Reject unknown properties (catch client typos) | `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow` |
| Enums in camelCase (`"admin"`) | `new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)` |
| Include nulls in responses | remove `DefaultIgnoreCondition = WhenWritingNull` |
| A custom type | add a `JsonConverter<T>` to `options.Converters` (see `UtcDateTimeConverter`) |

These change the contract for every client: treat them as breaking changes. If a custom converter hides a
type from the OpenAPI schema (it shows as `{}`), add a schema transformer like the one for `DateTime` in
`Common/OpenApi/OpenApiExtensions.cs`.

Tests: `tests/.../Common/Json/JsonDefaultsTests.cs` covers every rule above; add a case when you change one.
