using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>
/// Data annotation validation that reports the environment variable name,
/// e.g. "DB_NAME: The DB_NAME field is required." instead of the C# property name.
/// </summary>
internal sealed class EnvSettingsValidator<TSettings> : IValidateOptions<TSettings>
    where TSettings : class
{
    public ValidateOptionsResult Validate(string? name, TSettings options)
    {
        var errors = new List<string>();

        foreach (var property in typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var envKey = property.GetCustomAttribute<ConfigurationKeyNameAttribute>()?.Name;
            if (envKey is null)
            {
                continue;
            }

            var context = new ValidationContext(options) { MemberName = property.Name, DisplayName = envKey };
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateProperty(property.GetValue(options), context, results))
            {
                errors.AddRange(results.Select(r => $"{envKey}: {r.ErrorMessage}"));
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
