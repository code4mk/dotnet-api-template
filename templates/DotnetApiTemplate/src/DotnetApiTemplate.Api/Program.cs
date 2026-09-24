using DotnetApiTemplate.Api.Common.Extensions;
using DotnetApiTemplate.Api.Common.Middleware;
using DotnetApiTemplate.Api.Common.OpenApi;
using DotnetApiTemplate.Api.Common.Settings;

EnvFile.Load();                                             // root .env -> environment variables, APP_ENV -> environment

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiDefaults();                          // ProblemDetails, validation, OpenAPI/Swagger, JSON
builder.Services.AddInfrastructure(builder.Configuration);  // DbContext, auth, email
builder.Services.AddFeatures();                             // feature services

var app = builder.Build();
app.ValidateSettings();                                     // fail fast, listing every invalid env variable

app.UseMiddleware<CorrelationIdMiddleware>();               // first, so error logs and responses carry the id
app.UseExceptionHandler();                                  // exceptions -> ProblemDetails (GlobalExceptionHandler)
app.UseStatusCodePages();                                   // empty 4xx/5xx (e.g. 404) -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapApiDocs();                                       // /openapi/v1.json and Swagger UI at /swagger
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapFeatures();

await app.RunAsync();

// Makes Program visible to WebApplicationFactory in integration tests.
public partial class Program;
