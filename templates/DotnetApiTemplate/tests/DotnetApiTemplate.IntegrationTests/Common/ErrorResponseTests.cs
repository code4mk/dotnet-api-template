using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using DotnetApiTemplate.Api.Common.Middleware;

namespace DotnetApiTemplate.IntegrationTests.Common;

/// <summary>Bad requests and missing routes are answered with ProblemDetails, never a 500 or an empty body.</summary>
public sealed class ErrorResponseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task PostWithoutBody_Returns400ProblemDetails()
    {
        var response = await factory.CreateClient().PostAsync("/api/auth/login", content: null);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "bad_request");
        Assert.Equal("A JSON request body is required.", problem.Detail);
    }

    [Fact]
    public async Task PostWithInvalidJson_Returns400InvalidJson()
    {
        var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "invalid_json");
    }

    [Theory]
    [InlineData("""{"name":"Keyboard","description":"x","price":"49.99","stock":2}""", "$.price")]         // number as string
    [InlineData("""{"name":"A","name":"B","description":"x","price":49.99,"stock":2}""", "$.name")]      // duplicate property
    public async Task PostWithAmbiguousJson_Returns400WithPath(string json, string path)
    {
        var client = factory.CreateAuthenticatedClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/products", content);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "invalid_json");
        Assert.Contains($"at '{path}'", problem.Detail);
    }

    [Fact]
    public async Task InvalidRouteValue_ReturnsNotFoundProblem()
    {
        // {id:int} doesn't match, so no endpoint is found.
        var response = await factory.CreateClient().GetAsync("/api/products/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ErrorResponse_CarriesCorrelationIdFromHeader()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await client.PostAsync("/api/auth/login", content: null);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "bad_request");
        Assert.Equal("test-correlation-123", problem.Extensions[CorrelationIdMiddleware.ProblemDetailsKey]?.ToString());
        Assert.Equal("test-correlation-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    private static async Task<ProblemDetails> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(code, problem.Title);
        return problem;
    }
}
