using DotnetApiTemplate.Api.Common.Extensions;
using DotnetApiTemplate.Api.Common.Middleware;
using DotnetApiTemplate.Api.Common.OpenApi;
using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

EnvFile.Load();                                             // root .env -> environment variables, APP_ENV -> environment

var builder = WebApplication.CreateBuilder(args);

// In every environment: fail at startup if a registered service can't be created (e.g. a missing
// dependency) or a scoped service is used from a singleton, instead of on the first request.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

builder.Services.AddAllEnvSettings(builder.Configuration);  // every IEnvSettings class (DB_*, JWT_*, EMAIL_*, ...)
builder.Services.AddApiDefaults(builder.Configuration);     // ProblemDetails, validation, OpenAPI/Swagger, JSON, CORS
builder.Services.AddInfrastructure(builder.Configuration);  // DbContext, auth, email, background jobs
builder.Services.AddFeatures();                             // feature services

var app = builder.Build();
app.ValidateSettings();                                     // fail fast, listing every invalid env variable
var role = app.Services.GetRequiredService<AppSettings>();  // APP_ROLE: all | api | worker

app.UseMiddleware<CorrelationIdMiddleware>();               // first, so error logs and responses carry the id
app.UseCors();                                              // before errors and auth: preflights and error responses get CORS headers
app.UseExceptionHandler();                                  // exceptions -> ProblemDetails (GlobalExceptionHandler)
app.UseStatusCodePages();                                   // empty 4xx/5xx (e.g. 404) -> ProblemDetails

if (app.Environment.IsDevelopment() && role.RunsApi)
{
    app.MapApiDocs();                                       // /openapi/v1.json and Swagger UI at /swagger
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");                             // every role, also the worker (for health checks)

if (role.RunsApi)
{
    app.MapFeatures();                                      // GET / and every feature under /api
    app.MapJobsDashboard();                                 // /jobs: open in Development, basic auth elsewhere
}

app.Logger.LogInformation("Role {Role}: API {Api}, background jobs {Jobs}", role.Role, role.RunsApi, role.RunsJobs);

await app.RunAsync();

// Makes Program visible to WebApplicationFactory in integration tests.
public partial class Program;
