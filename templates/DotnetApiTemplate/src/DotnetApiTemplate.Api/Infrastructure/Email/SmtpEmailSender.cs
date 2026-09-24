using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>
/// Simple SMTP sender. For high volume or advanced features, replace with MailKit
/// or a provider SDK (SendGrid, Amazon SES, ...) behind the same interface.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(_options.From, to, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrEmpty(_options.UserName)
                ? null
                : new NetworkCredential(_options.UserName, _options.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
