using System.Net;
using System.Net.Http.Json;
using DotnetApiTemplate.Api.Features.Root;

namespace DotnetApiTemplate.IntegrationTests.Features.Root;

public sealed class RootEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetRoot_IsPublic_ReturnsWelcomeMessage()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RootResponse>();
        Assert.NotNull(body);
        Assert.Equal("DotnetApiTemplate API", body.Name);
        Assert.Contains("up and running", body.Message);
        Assert.Equal("/health", body.Links["health"]);
    }
}
