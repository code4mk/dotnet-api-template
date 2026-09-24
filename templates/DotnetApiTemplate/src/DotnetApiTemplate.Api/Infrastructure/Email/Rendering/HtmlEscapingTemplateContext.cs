using System.Net;
using Scriban;
using Scriban.Parsing;

namespace DotnetApiTemplate.Api.Infrastructure.Email.Rendering;

/// <summary>
/// Scriban does not escape HTML by default. This context HTML-encodes the output of every
/// <c>{{ expression }}</c>, so user data can't inject markup. Literal template text is written as is.
/// Mark trusted HTML with <c>{{ value | raw }}</c> (or pass a <see cref="RawHtml"/> value).
/// </summary>
internal sealed class HtmlEscapingTemplateContext : TemplateContext
{
    public override TemplateContext Write(SourceSpan span, object? textAsObject)
    {
        if (textAsObject is not null)
        {
            Write(Encode(textAsObject));
        }

        return this;
    }

    public override async ValueTask<TemplateContext> WriteAsync(SourceSpan span, object? textAsObject)
    {
        if (textAsObject is not null)
        {
            await WriteAsync(Encode(textAsObject));
        }

        return this;
    }

    private string Encode(object value) =>
        value is RawHtml raw ? raw.Value : WebUtility.HtmlEncode(ObjectToString(value, nested: false) ?? string.Empty);
}

/// <summary>HTML that is written without escaping. Only use it for markup you control.</summary>
public sealed record RawHtml(string Value)
{
    public override string ToString() => Value;
}
