namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>Renders a typed email and sends it. This is what features use.</summary>
public interface IEmailService
{
    Task SendAsync(string to, EmailTemplate email, CancellationToken cancellationToken = default);
}
