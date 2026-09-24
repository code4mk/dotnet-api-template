using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace DotnetApiTemplate.IntegrationTests.Common;

public sealed class OpenApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Swagger and the OpenAPI document are only mapped in Development.</summary>
    private HttpClient CreateDevelopmentClient() =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();

    [Fact]
    public async Task SwaggerUi_InDevelopment_IsServed()
    {
        var response = await CreateDevelopmentClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_MarksOnlyProtectedOperationsWithBearer()
    {
        using var document = JsonDocument.Parse(await CreateDevelopmentClient().GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;

        Assert.True(root.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
        Assert.True(HasSecurity(root, "/api/users", "get"));
        Assert.True(HasSecurity(root, "/api/products", "post"));
        Assert.False(HasSecurity(root, "/api/products", "get"));
        Assert.False(HasSecurity(root, "/api/auth/login", "post"));
    }

    [Fact]
    public async Task SwaggerUi_OutsideDevelopment_IsNotServed()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static bool HasSecurity(JsonElement root, string path, string method) =>
        root.GetProperty("paths").GetProperty(path).GetProperty(method).TryGetProperty("security", out _);
}
