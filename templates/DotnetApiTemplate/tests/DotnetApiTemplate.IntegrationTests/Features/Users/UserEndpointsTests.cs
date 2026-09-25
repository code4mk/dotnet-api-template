using System.Net;
using System.Net.Http.Json;
using DotnetApiTemplate.Api.Common.Json;
using DotnetApiTemplate.Api.Features.Users;
using DotnetApiTemplate.Api.Features.Users.Jobs;

namespace DotnetApiTemplate.IntegrationTests.Features.Users;

public sealed class UserEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task CreateUser_IsPublic_ReturnsCreated()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("Jane Doe", $"jane-{Guid.NewGuid():N}@example.com", "Password@123"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_EnqueuesWelcomeEmail_ThatRendersAndSends()
    {
        var client = factory.CreateClient();
        var email = $"welcome-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest("Jane Doe", email, "Password@123"));
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonDefaults.Options);

        // The request only enqueues the job; run it like a worker would.
        await factory.Jobs.RunAsync(factory.Services, job => job.Type == typeof(SendWelcomeEmailJob) && Equals(job.Args[0], user!.Id));

        var message = Assert.Single(factory.Emails.Sent, m => m.To == email);
        Assert.Equal("Welcome, Jane Doe!", message.Subject);
        Assert.Contains("style=\"", message.HtmlBody);
        Assert.Contains("Welcome, Jane Doe!", message.TextBody);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var request = new CreateUserRequest("Jane Doe", $"dup-{Guid.NewGuid():N}@example.com", "Password@123");

        await client.PostAsJsonAsync("/api/users", request);
        var response = await client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsNonAdmin_ReturnsForbidden()
    {
        var client = factory.CreateAuthenticatedClient(role: "User");

        var response = await client.DeleteAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
