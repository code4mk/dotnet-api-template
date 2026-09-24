namespace DotnetApiTemplate.Api.Infrastructure.Email.Rendering;

public interface IEmailRenderer
{
    Task<RenderedEmail> RenderAsync(EmailTemplate email, CancellationToken cancellationToken = default);
}

/// <summary>Subject, HTML with inlined CSS, and the plain-text alternative.</summary>
public sealed record RenderedEmail(string Subject, string Html, string Text);
