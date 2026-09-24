using System.Net;
using System.Text.RegularExpressions;

namespace DotnetApiTemplate.Api.Infrastructure.Email.Rendering;

/// <summary>
/// Builds the plain-text part from rendered email HTML when a template has no <c>.txt.scriban</c> file.
/// Good enough for our own simple layouts; write a text template for anything complex.
/// </summary>
internal static partial class HtmlToText
{
    public static string Convert(string html)
    {
        var text = HeadOrStyle().Replace(html, string.Empty);
        text = Link().Replace(text, m => m.Groups["text"].Value.Trim() == m.Groups["href"].Value
            ? m.Groups["href"].Value
            : $"{m.Groups["text"].Value} ({m.Groups["href"].Value})");
        text = LineBreak().Replace(text, "\n");
        text = BlockEnd().Replace(text, "\n\n");
        text = Tag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);

        var lines = text.Split('\n').Select(line => Spaces().Replace(line, " ").Trim());
        return BlankLines().Replace(string.Join('\n', lines), "\n\n").Trim();
    }

    [GeneratedRegex(@"<(head|style|script)\b.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HeadOrStyle();

    [GeneratedRegex(@"<a\b[^>]*\bhref=""(?<href>[^""]*)""[^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Link();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"</(p|div|h[1-6]|li|tr|table)>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEnd();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"[ \t\r\f\v]+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();
}
