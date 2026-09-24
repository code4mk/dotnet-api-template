namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>Writes emails to the log instead of sending them. Used when no SMTP host is configured.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email to {To} with subject {Subject}:\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
