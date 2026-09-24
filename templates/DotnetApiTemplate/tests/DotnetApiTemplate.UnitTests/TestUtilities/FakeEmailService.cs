using DotnetApiTemplate.Api.Infrastructure.Email;

namespace DotnetApiTemplate.UnitTests.TestUtilities;

/// <summary>Records emails instead of sending them.</summary>
internal sealed class FakeEmailService : IEmailService
{
    public List<(string To, EmailTemplate Email)> Sent { get; } = [];

    public Task SendAsync(string to, EmailTemplate email, CancellationToken cancellationToken = default)
    {
        Sent.Add((to, email));
        return Task.CompletedTask;
    }
}
