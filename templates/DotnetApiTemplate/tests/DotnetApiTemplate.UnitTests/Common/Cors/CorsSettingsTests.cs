using DotnetApiTemplate.Api.Common.Cors;
using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.UnitTests.Common.Cors;

public sealed class CorsSettingsTests
{
    private readonly EnvSettingsValidator<CorsSettings> _validator = new();

    [Fact]
    public void Origins_ParsesCommaSeparatedListAndTrimsSlashes()
    {
        var settings = new CorsSettings { AllowedOrigins = " https://app.example.com/ , http://localhost:5173 ,," };

        Assert.Equal(["https://app.example.com", "http://localhost:5173"], settings.Origins);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://app.example.com")]
    [InlineData("http://localhost:5173,http://localhost:3000")]
    public void Validate_AcceptsEmptyAndExactOrigins(string value)
    {
        var result = _validator.Validate(null, new CorsSettings { AllowedOrigins = value });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("*", "wildcards")]
    [InlineData("https://*.example.com", "wildcards")]
    [InlineData("app.example.com", "scheme://host")]
    [InlineData("ftp://example.com", "http and https")]
    [InlineData("https://app.example.com/login", "no path")]
    public void Validate_RejectsWildcardsAndMalformedOrigins(string value, string expectedMessagePart)
    {
        var result = _validator.Validate(null, new CorsSettings { AllowedOrigins = value });

        Assert.True(result.Failed);
        var failure = Assert.Single(result.Failures!);
        Assert.StartsWith("CORS_ALLOWED_ORIGINS:", failure);
        Assert.Contains(expectedMessagePart, failure);
    }
}
