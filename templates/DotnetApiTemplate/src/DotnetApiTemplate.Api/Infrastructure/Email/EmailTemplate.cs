namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>
/// Base class for a typed email. The subclass is the template model: its public properties are available
/// in the Scriban template in snake_case (<c>FullName</c> → <c>{{ full_name }}</c>).
/// </summary>
/// <remarks>
/// Put the template next to the class, named after it: <c>WelcomeEmail.html.scriban</c> (required) and
/// optionally <c>WelcomeEmail.txt.scriban</c> for the plain-text part (otherwise generated from the HTML).
/// Templates are embedded resources, so file names must be unique across the project.
/// </remarks>
public abstract class EmailTemplate
{
    /// <summary>Subject line (plain text).</summary>
    public abstract string Subject { get; }

    /// <summary>Template file name without extension. Defaults to the class name.</summary>
    public virtual string TemplateName => GetType().Name;
}
