using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Options;
using PreMailer.Net;
using Scriban;
using Scriban.Runtime;

namespace DotnetApiTemplate.Api.Infrastructure.Email.Rendering;

/// <summary>
/// Scriban template (HTML-escaped) → shared layout → CSS inlined with PreMailer.Net, plus a plain-text part.
/// Templates are embedded resources (see the .csproj), parsed once and cached. In Development they are read
/// from the source files on every render instead, so template edits show up without a restart.
/// Never render templates that come from users: they are code.
/// </summary>
internal sealed class ScribanEmailRenderer(
    IOptions<EmailSettings> options,
    TimeProvider timeProvider,
    IHostEnvironment environment) : IEmailRenderer
{
    private const string ResourcePrefix = "email-templates/";
    private const string LayoutTemplate = "_layout.html.scriban";
    private const string Stylesheet = "email.css";

    private static readonly Assembly ResourceAssembly = typeof(ScribanEmailRenderer).Assembly;
    private static readonly ConcurrentDictionary<string, Template?> Templates = new();

    private readonly EmailSettings _settings = options.Value;

    public async Task<RenderedEmail> RenderAsync(EmailTemplate email, CancellationToken cancellationToken = default)
    {
        var bodyTemplate = GetTemplate($"{email.TemplateName}.html.scriban")
            ?? throw new InvalidOperationException(
                $"Email template '{email.TemplateName}.html.scriban' not found. Put it next to {email.GetType().Name}.cs.");

        var body = await RenderHtmlAsync(bodyTemplate, email, content: null);
        var layout = await RenderHtmlAsync(GetTemplate(LayoutTemplate)!, email, content: new RawHtml(body));

        var html = PreMailer.Net.PreMailer.MoveCssInline(
            layout,
            removeStyleElements: true,
            css: ReadSource(Stylesheet),
            removeComments: true,
            preserveMediaQueries: true).Html;

        var textTemplate = GetTemplate($"{email.TemplateName}.txt.scriban");
        var text = textTemplate is null
            ? HtmlToText.Convert(html)
            : await textTemplate.RenderAsync(CreateContext(new TemplateContext(), email, content: null));

        return new RenderedEmail(email.Subject, html, text.Trim());
    }

    private Task<string> RenderHtmlAsync(Template template, EmailTemplate email, RawHtml? content) =>
        template.RenderAsync(CreateContext(new HtmlEscapingTemplateContext(), email, content)).AsTask();

    /// <summary>Template variables: the email's properties (snake_case) plus app_name, year, content and raw().</summary>
    private TemplateContext CreateContext(TemplateContext context, EmailTemplate email, RawHtml? content)
    {
        var globals = new ScriptObject();
        globals.Import(email, renamer: StandardMemberRenamer.Default);
        globals.SetValue("app_name", _settings.FromName, readOnly: true);
        globals.SetValue("year", timeProvider.GetUtcNow().Year, readOnly: true);
        globals.SetValue("content", content, readOnly: true);
        globals.Import("raw", new Func<object?, RawHtml>(value => new RawHtml(value?.ToString() ?? string.Empty)));

        context.MemberRenamer = StandardMemberRenamer.Default;
        context.PushGlobal(globals);
        return context;
    }

    private Template? GetTemplate(string fileName) =>
        environment.IsDevelopment()
            ? Parse(fileName, ReadSource(fileName))
            : Templates.GetOrAdd(fileName, static name => Parse(name, ReadResource(name)));

    private static Template? Parse(string fileName, string? source)
    {
        if (source is null)
        {
            return null;
        }

        var template = Template.Parse(source, fileName);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Email template '{fileName}' has errors: {string.Join("; ", template.Messages)}");
        }

        return template;
    }

    /// <summary>In Development, the source file under the content root (if present); otherwise the embedded resource.</summary>
    private string? ReadSource(string fileName)
    {
        if (environment.IsDevelopment())
        {
            var path = Directory
                .EnumerateFiles(environment.ContentRootPath, fileName, SearchOption.AllDirectories)
                .FirstOrDefault(p => !IsBuildOutput(Path.GetRelativePath(environment.ContentRootPath, p)));

            if (path is not null)
            {
                return File.ReadAllText(path);
            }
        }

        return ReadResource(fileName);
    }

    private static bool IsBuildOutput(string relativePath) =>
        relativePath.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || relativePath.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string? ReadResource(string fileName)
    {
        using var stream = ResourceAssembly.GetManifestResourceStream(ResourcePrefix + fileName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
