namespace DotnetApiTemplate.Api.Features.Root;

public sealed record RootResponse(
    string Name,
    string Message,
    string Environment,
    string Version,
    IReadOnlyDictionary<string, string> Links);
