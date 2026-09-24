namespace DotnetApiTemplate.Api.Infrastructure.Authentication;

/// <summary>Authorization policy names. Use these constants, never string literals.</summary>
public static class Policies
{
    public const string Admin = "Admin";
}

/// <summary>Claim types written into tokens.</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
