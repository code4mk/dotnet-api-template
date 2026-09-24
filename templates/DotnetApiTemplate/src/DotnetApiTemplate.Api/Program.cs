using DotnetApiTemplate.Api.Common.Extensions;
using DotnetApiTemplate.Api.Common.Middleware;
using DotnetApiTemplate.Api.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiDefaults();                          // ProblemDetails, validation, OpenAPI, JSON
builder.Services.AddInfrastructure(builder.Configuration);  // DbContext, auth, email
builder.Services.AddFeatures();                             // feature services

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.InitializeDatabaseAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapFeatures();

await app.RunAsync();

// Makes Program visible to WebApplicationFactory in integration tests.
public partial class Program;
