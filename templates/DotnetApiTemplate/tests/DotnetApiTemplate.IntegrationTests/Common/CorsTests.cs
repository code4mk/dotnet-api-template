using System.Net;
using Microsoft.AspNetCore.Hosting;
using DotnetApiTemplate.Api.Common.Middleware;

namespace DotnetApiTemplate.IntegrationTests.Common;

public sealed class CorsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string AllowedOrigin = "http://localhost:5173";

    private HttpClient CreateClient(string allowedOrigins) =>
        factory.WithWebHostBuilder(builder => builder.UseSetting("CORS_ALLOWED_ORIGINS", allowedOrigins)).CreateClient();

    [Fact]
    public async Task Preflight_FromAllowedOrigin_IsAllowedAndCached()
    {
        var response = await CreateClient(AllowedOrigin).SendAsync(Preflight("/api/products", AllowedOrigin, "POST", "authorization,content-type"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, Header(response, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", Header(response, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", Header(response, "Access-Control-Allow-Headers")!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("600", Header(response, "Access-Control-Max-Age"));
        Assert.Null(Header(response, "Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Request_FromAllowedOrigin_ExposesCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await CreateClient(AllowedOrigin).SendAsync(request);

        Assert.Equal(AllowedOrigin, Header(response, "Access-Control-Allow-Origin"));
        Assert.Contains(CorrelationIdMiddleware.HeaderName, Header(response, "Access-Control-Expose-Headers"));
    }

    [Fact]
    public async Task ErrorResponse_FromAllowedOrigin_StillHasCorsHeaders()
    {
        // Without CORS headers on errors, the browser hides the ProblemDetails body from the frontend.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await CreateClient(AllowedOrigin).SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AllowedOrigin, Header(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_GetsNoCorsHeaders()
    {
        var response = await CreateClient(AllowedOrigin).SendAsync(Preflight("/api/products", "https://evil.example", "POST", "content-type"));

        Assert.Null(Header(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task NoOriginsConfigured_SendsNoCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await CreateClient(string.Empty).SendAsync(request);

        Assert.Null(Header(response, "Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage Preflight(string path, string origin, string method, string headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", headers);
        return request;
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
