using Microsoft.OpenApi;

namespace DotnetApiTemplate.Api.Common.OpenApi;

public static class OpenApiExtensions
{
    private const string DocumentName = "v1";
    private const string Title = "DotnetApiTemplate API";

    /// <summary>OpenAPI document (built-in generator) with API info and JWT bearer security.</summary>
    public static IServiceCollection AddApiDocs(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = Title,
                    Version = DocumentName,
                    Description = "Log in with POST /api/auth/login, then use Authorize with the access token.",
                };
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<BearerSecurityTransformer>();
            options.AddOperationTransformer<BearerSecurityTransformer>();

            // UtcDateTimeConverter hides the type from the schema generator: describe DateTime as an ISO 8601 string.
            options.AddSchemaTransformer((schema, context, _) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    schema.Type = type == typeof(DateTime?) ? JsonSchemaType.String | JsonSchemaType.Null : JsonSchemaType.String;
                    schema.Format = "date-time";
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Serves the OpenAPI document at /openapi/v1.json and Swagger UI at /swagger.
    /// Called for Development only (see Program.cs).
    /// </summary>
    public static WebApplication MapApiDocs(this WebApplication app)
    {
        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/openapi/{DocumentName}.json", $"{Title} {DocumentName}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = Title;
            options.EnablePersistAuthorization();   // keep the token across page reloads
            options.DisplayRequestDuration();
        });

        return app;
    }
}
