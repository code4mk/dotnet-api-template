using DotnetApiTemplate.Api.Infrastructure.Email;

namespace DotnetApiTemplate.Api.Features.Users.Emails;

/// <summary>Sent when a user account is created. Template: WelcomeEmail.html.scriban.</summary>
public sealed class WelcomeEmail(string fullName, string email) : EmailTemplate
{
    public string FullName { get; } = fullName;

    public string Email { get; } = email;

    public override string Subject => $"Welcome, {FullName}!";
}
