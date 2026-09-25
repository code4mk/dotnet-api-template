namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>
/// Marks a settings class bound from environment variables. Every class implementing it, anywhere in the
/// project, is registered and validated at startup by <c>AddAllEnvSettings()</c>: no registration line needed.
/// </summary>
public interface IEnvSettings;
