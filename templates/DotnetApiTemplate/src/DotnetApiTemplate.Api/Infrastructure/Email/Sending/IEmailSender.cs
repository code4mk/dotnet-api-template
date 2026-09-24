namespace DotnetApiTemplate.Api.Infrastructure.Email.Sending;

/// <summary>Delivers an already rendered email. Features use <see cref="IEmailService"/> instead.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
