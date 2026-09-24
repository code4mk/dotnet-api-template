using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Common.Cors;

/// <summary>
/// Validates a comma-separated list of exact origins (<c>scheme://host[:port]</c>). Wildcards, paths and
/// non-http(s) schemes fail at startup, so a typo can't silently open the API to every site.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class OriginListAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        foreach (var origin in CorsSettings.Parse(value as string))
        {
            if (Problem(origin) is { } problem)
            {
                return new ValidationResult(
                    $"The field {validationContext.DisplayName} has an invalid origin '{origin}': {problem}.",
                    [validationContext.MemberName ?? string.Empty]);
            }
        }

        return ValidationResult.Success;
    }

    private static string? Problem(string origin)
    {
        if (origin.Contains('*'))
        {
            return "wildcards are not allowed, list each origin";
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return "expected scheme://host[:port], e.g. https://app.example.com";
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return "only http and https are allowed";
        }

        if (uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0)
        {
            return "an origin has no path, query or fragment";
        }

        return null;
    }
}
