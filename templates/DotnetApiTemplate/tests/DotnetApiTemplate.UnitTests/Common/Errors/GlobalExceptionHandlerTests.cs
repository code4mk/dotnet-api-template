using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using DotnetApiTemplate.Api.Common.Errors;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Common.Errors;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task UnknownException_Returns500WithoutInternals()
    {
        var (status, body) = await HandleAsync(new InvalidOperationException("secret connection string"));

        Assert.Equal(500, status);
        Assert.Equal("server_error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("secret", body.GetRawText());
    }

    [Fact]
    public async Task MissingBody_Returns400WithFriendlyMessage()
    {
        var exception = new BadHttpRequestException(
            "Implicit body inferred for parameter \"request\" but no body was provided. Did you mean to use a Service instead?");

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(400, status);
        Assert.Equal("bad_request", body.GetProperty("title").GetString());
        Assert.Equal("A JSON request body is required.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvalidJson_Returns400InvalidJson()
    {
        var exception = new BadHttpRequestException("Failed to read parameter", new JsonException("bad token"));

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(400, status);
        Assert.Equal("invalid_json", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PayloadTooLarge_KeepsFrameworkStatusCode()
    {
        var (status, body) = await HandleAsync(new BadHttpRequestException("Request body too large.", 413));

        Assert.Equal(413, status);
        Assert.Equal("payload_too_large", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UniqueViolation_Returns409Conflict()
    {
        var postgres = new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);

        var (status, body) = await HandleAsync(new DbUpdateException("save failed", postgres));

        Assert.Equal(409, status);
        Assert.Equal("conflict", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ConcurrencyConflict_Returns409()
    {
        var (status, body) = await HandleAsync(new DbUpdateConcurrencyException("row changed"));

        Assert.Equal(409, status);
        Assert.Equal("concurrency_conflict", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AbortedRequest_Returns499WithoutBody()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var (status, body) = await HandleAsync(new OperationCanceledException(), requestAborted: aborted.Token);

        Assert.Equal(499, status);
        Assert.Equal(JsonValueKind.Undefined, body.ValueKind);
    }

    [Fact]
    public async Task Development_IncludesExceptionDetails()
    {
        var (_, body) = await HandleAsync(new InvalidOperationException("boom"), environmentName: "Development");

        var exception = body.GetProperty("exception");
        Assert.Equal("System.InvalidOperationException", exception.GetProperty("type").GetString());
        Assert.Equal("boom", exception.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Production_HidesExceptionDetails()
    {
        var (_, body) = await HandleAsync(new InvalidOperationException("boom"), environmentName: "Production");

        Assert.False(body.TryGetProperty("exception", out _));
    }

    private static async Task<(int Status, JsonElement Body)> HandleAsync(
        Exception exception,
        string environmentName = "Production",
        CancellationToken requestAborted = default)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services, RequestAborted = requestAborted };
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/test";
        httpContext.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            new TestHostEnvironment(environmentName),
            NullLogger<GlobalExceptionHandler>.Instance);

        Assert.True(await handler.TryHandleAsync(httpContext, exception, CancellationToken.None));

        httpContext.Response.Body.Position = 0;
        var text = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        var body = text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (httpContext.Response.StatusCode, body);
    }
}
