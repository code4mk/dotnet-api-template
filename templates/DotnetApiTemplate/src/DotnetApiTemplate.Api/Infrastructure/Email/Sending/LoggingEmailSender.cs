namespace DotnetApiTemplate.Api.Infrastructure.Email.Sending;

/// <summary>Writes emails to the log instead of sending them. Used when EMAIL_HOST is empty.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email to {To} with subject {Subject} (not sent, EMAIL_HOST is empty):\n{TextBody}",
            message.To, message.Subject, message.TextBody);
        return Task.CompletedTask;
    }
}
