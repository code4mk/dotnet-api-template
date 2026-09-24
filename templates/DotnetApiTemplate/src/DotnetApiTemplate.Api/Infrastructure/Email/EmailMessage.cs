namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>A rendered email, ready to send.</summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);
