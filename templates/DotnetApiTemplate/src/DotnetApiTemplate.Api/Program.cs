using DotnetApiTemplate.Api.Common.Extensions;
using DotnetApiTemplate.Api.Common.Middleware;
using DotnetApiTemplate.Api.Common.OpenApi;
using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Data.Seed;

EnvFile.Load();                                             // root .env -> environment variables, APP_ENV -> environment

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiDefaults();                          // ProblemDetails, validation, OpenAPI/Swagger, JSON
builder.Services.AddInfrastructure(builder.Configuration);  // DbContext, auth, email
builder.Services.AddFeatures();                             // feature services

var app = builder.Build();
app.ValidateSettings();                                     // fail fast, listing every invalid env variable

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapApiDocs();                                       // /openapi/v1.json and Swagger UI at /swagger
    await app.InitializeDatabaseAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapFeatures();

await app.RunAsync();

// Makes Program visible to WebApplicationFactory in integration tests.
public partial class Program;
