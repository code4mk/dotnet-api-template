using DotnetApiTemplate.Api.Infrastructure.Email.Rendering;
using DotnetApiTemplate.Api.Infrastructure.Email.Sending;

namespace DotnetApiTemplate.Api.Infrastructure.Email;

internal sealed class EmailService(IEmailRenderer renderer, IEmailSender sender) : IEmailService
{
    public async Task SendAsync(string to, EmailTemplate email, CancellationToken cancellationToken = default)
    {
        var rendered = await renderer.RenderAsync(email, cancellationToken);
        await sender.SendAsync(new EmailMessage(to, rendered.Subject, rendered.Html, rendered.Text), cancellationToken);
    }
}
