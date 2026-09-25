using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

/// <summary>
/// HTTP basic auth for the Hangfire dashboard with JOBS_DASHBOARD_USERNAME / JOBS_DASHBOARD_PASSWORD.
/// The dashboard is a browser page, so the API's bearer token can't protect it. Serve it over HTTPS only.
/// </summary>
internal sealed class DashboardBasicAuthFilter(string username, string password) : IDashboardAuthorizationFilter
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes($"{username}:{password}");

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (IsAuthorized(httpContext.Request.Headers.Authorization.ToString()))
        {
            return true;
        }

        // Ask the browser for credentials. Hangfire then answers 401 itself.
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Jobs\", charset=\"UTF-8\"";
        return false;
    }

    internal bool IsAuthorized(string authorizationHeader)
    {
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var header)
            || !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(header.Parameter))
        {
            return false;
        }

        byte[] credentials;
        try
        {
            credentials = Convert.FromBase64String(header.Parameter);
        }
        catch (FormatException)
        {
            return false;
        }

        // Constant-time comparison: no timing hints about how much of the password matched.
        return CryptographicOperations.FixedTimeEquals(credentials, _expected);
    }
}

/// <summary>Development only: everyone may open the dashboard.</summary>
internal sealed class DashboardOpenFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
