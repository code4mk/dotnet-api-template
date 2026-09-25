using Microsoft.Extensions.Logging.Abstractions;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Domain.Entities;
using DotnetApiTemplate.Api.Features.Users.Emails;
using DotnetApiTemplate.Api.Features.Users.Jobs;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Features.Users;

public sealed class SendWelcomeEmailJobTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly FakeEmailService _emails = new();
    private readonly SendWelcomeEmailJob _sut;

    public SendWelcomeEmailJobTests() => _sut = new SendWelcomeEmailJob(_db, _emails, NullLogger<SendWelcomeEmailJob>.Instance);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ExecuteAsync_ForExistingUser_SendsWelcomeEmail()
    {
        var user = new User { FullName = "Jane Doe", Email = "jane@example.com", PasswordHash = "x" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _sut.ExecuteAsync(user.Id, CancellationToken.None);

        var (to, email) = Assert.Single(_emails.Sent);
        Assert.Equal("jane@example.com", to);
        Assert.Equal("Jane Doe", Assert.IsType<WelcomeEmail>(email).FullName);
    }

    [Fact]
    public async Task ExecuteAsync_ForDeletedUser_DoesNothing()
    {
        await _sut.ExecuteAsync(12345, CancellationToken.None);   // must not throw: retrying could never succeed

        Assert.Empty(_emails.Sent);
    }
}
