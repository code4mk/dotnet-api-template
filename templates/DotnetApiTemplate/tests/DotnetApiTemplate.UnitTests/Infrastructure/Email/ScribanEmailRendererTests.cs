using Microsoft.Extensions.Options;
using DotnetApiTemplate.Api.Features.Users.Emails;
using DotnetApiTemplate.Api.Infrastructure.Email;
using DotnetApiTemplate.Api.Infrastructure.Email.Rendering;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Infrastructure.Email;

public sealed class ScribanEmailRendererTests
{
    private readonly ScribanEmailRenderer _sut = new(
        Options.Create(new EmailSettings { FromName = "Acme" }),
        TimeProvider.System,
        new TestHostEnvironment());

    [Fact]
    public async Task RenderAsync_UsesSubjectAndModelValues()
    {
        var result = await _sut.RenderAsync(new WelcomeEmail("Jane Doe", "jane@example.com"));

        Assert.Equal("Welcome, Jane Doe!", result.Subject);
        Assert.Contains("Welcome, Jane Doe!", result.Html);
        Assert.Contains("jane@example.com", result.Html);
    }

    [Fact]
    public async Task RenderAsync_EscapesHtmlInModelValues()
    {
        var result = await _sut.RenderAsync(new WelcomeEmail("<script>alert(1)</script>", "x@example.com"));

        Assert.DoesNotContain("<script>", result.Html);
        Assert.Contains("&lt;script&gt;", result.Html);
    }

    [Fact]
    public async Task RenderAsync_WrapsBodyInLayoutWithAppName()
    {
        var result = await _sut.RenderAsync(new WelcomeEmail("Jane", "jane@example.com"));

        Assert.StartsWith("<!DOCTYPE html>", result.Html.TrimStart(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Acme", result.Html);
        Assert.Contains($"{DateTime.UtcNow.Year}", result.Html);
    }

    [Fact]
    public async Task RenderAsync_InlinesCssAndRemovesStyleBlocks()
    {
        var result = await _sut.RenderAsync(new WelcomeEmail("Jane", "jane@example.com"));

        Assert.DoesNotContain("<style", result.Html);
        Assert.Contains("style=\"", result.Html);
        Assert.Contains("background-color: #f4f5f7", result.Html);
    }

    [Fact]
    public async Task RenderAsync_BuildsPlainTextWithoutTags()
    {
        var result = await _sut.RenderAsync(new WelcomeEmail("Jane & co", "jane@example.com"));

        Assert.Contains("Welcome, Jane & co!", result.Text);
        Assert.DoesNotContain("<", result.Text);
    }
}
