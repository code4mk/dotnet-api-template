using System.Collections.Concurrent;
using DotnetApiTemplate.Api.Infrastructure.Email;
using DotnetApiTemplate.Api.Infrastructure.Email.Sending;

namespace DotnetApiTemplate.IntegrationTests.TestUtilities;

/// <summary>Captures rendered emails instead of sending them.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
