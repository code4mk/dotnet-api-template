using Microsoft.AspNetCore.Identity;
using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Domain.Entities;
using DotnetApiTemplate.Api.Features.Users;
using DotnetApiTemplate.Api.Features.Users.Jobs;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Features.Users;

public sealed class UserServiceTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly PasswordHasher<User> _hasher = new();
    private readonly FakeBackgroundJobClient _jobs = new();
    private readonly UserService _sut;

    public UserServiceTests() => _sut = new UserService(_db, _hasher, _jobs);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_WithNewEmail_EnqueuesWelcomeEmailJob()
    {
        var result = await _sut.CreateAsync(new CreateUserRequest("Jane Doe", "Jane@Example.com", "Password@123"), CancellationToken.None);

        var job = Assert.Single(_jobs.Jobs);
        Assert.Equal(typeof(SendWelcomeEmailJob), job.Type);
        Assert.Equal(nameof(SendWelcomeEmailJob.ExecuteAsync), job.Method.Name);
        Assert.Equal(result.Value.Id, job.Args[0]);
    }

    [Fact]
    public async Task CreateAsync_WithNewEmail_CreatesUserWithHashedPassword()
    {
        var request = new CreateUserRequest("Jane Doe", " Jane@Example.com ", "Password@123");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jane@example.com", result.Value.Email);

        var saved = Assert.Single(_db.Users);
        Assert.NotEqual("Password@123", saved.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            _hasher.VerifyHashedPassword(saved, saved.PasswordHash, "Password@123"));
    }

    [Fact]
    public async Task CreateAsync_WithExistingEmail_ReturnsConflict()
    {
        await _sut.CreateAsync(new CreateUserRequest("Jane", "jane@example.com", "Password@123"), CancellationToken.None);

        var result = await _sut.CreateAsync(new CreateUserRequest("Other", "JANE@example.com", "Password@123"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal("users.email_exists", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserExists_UpdatesFields()
    {
        var created = await _sut.CreateAsync(new CreateUserRequest("Jane", "jane@example.com", "Password@123"), CancellationToken.None);

        var result = await _sut.UpdateAsync(created.Value.Id, new UpdateUserRequest("Jane Smith", false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Smith", result.Value.FullName);
        Assert.False(result.Value.IsActive);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_RemovesUser()
    {
        var created = await _sut.CreateAsync(new CreateUserRequest("Jane", "jane@example.com", "Password@123"), CancellationToken.None);

        var result = await _sut.DeleteAsync(created.Value.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_db.Users);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        for (var i = 1; i <= 3; i++)
        {
            await _sut.CreateAsync(new CreateUserRequest($"User {i}", $"user{i}@example.com", "Password@123"), CancellationToken.None);
        }

        var page = await _sut.GetAllAsync(page: 1, pageSize: 2, CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNextPage);
    }
}
