using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DotnetApiTemplate.Api.Infrastructure.Email.Sending;

/// <summary>Sends email over SMTP with MailKit, as multipart HTML + plain text.</summary>
internal sealed class MailKitEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.From));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptionsFor(_settings), cancellationToken);

        if (!string.IsNullOrEmpty(_settings.UserName))
        {
            await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static SecureSocketOptions SecureSocketOptionsFor(EmailSettings settings) => settings switch
    {
        { EnableSsl: false } => SecureSocketOptions.None,
        { Port: 465 } => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.StartTls,
    };
}
