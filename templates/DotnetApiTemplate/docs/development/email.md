# Email

Why these libraries: [ADR-0003](../adr/0003-send-email-with-scriban-premailer-mailkit.md).

Emails are **typed classes** with a **Scriban template**. The pipeline:

```text
WelcomeEmail (C# model + subject)
  → Scriban renders WelcomeEmail.html.scriban (every {{ value }} HTML-escaped)
  → wrapped in Layout/_layout.html.scriban (header, footer)
  → PreMailer.Net inlines Layout/email.css into style="" attributes (Gmail/Outlook drop <style>)
  → plain-text part from WelcomeEmail.txt.scriban, or generated from the HTML
  → MailKit sends HTML + text over SMTP   (or it's only logged when EMAIL_HOST is empty)
```

## Folder layout

```text
Infrastructure/Email/
├── IEmailService.cs, EmailService.cs   SendAsync(to, email): render + send. This is what features use.
├── EmailTemplate.cs                    base class for typed emails
├── EmailMessage.cs                     a rendered email (to, subject, html, text)
├── EmailSettings.cs                    EMAIL_* settings
├── EmailExtensions.cs                  registration; picks MailKit or the logging sender
├── Rendering/                          Scriban renderer, auto-escaping, HTML → text
├── Sending/                            MailKitEmailSender, LoggingEmailSender, IEmailSender
└── Layout/
    ├── _layout.html.scriban            shared layout for every email
    └── email.css                       shared styles (inlined)
Features/<Feature>/Emails/
├── WelcomeEmail.cs                     model + subject
├── WelcomeEmail.html.scriban           body (required)
└── WelcomeEmail.txt.scriban            plain text (optional)
```

## See emails locally (Mailpit)

Development sends every email to [Mailpit](https://mailpit.axllent.org/), a local fake SMTP server with a
web inbox. Nothing reaches real inboxes.

```bash
docker compose up -d mailpit
```

Open the inbox at `http://localhost:28025` (`MAILPIT_UI_PORT`). Register a user (`POST /api/users`) to
see the sample welcome email, with its HTML, text part and an HTML compatibility check.

Without Mailpit, set `EMAIL_HOST=` (empty) in `.env`: emails are then written to the log instead.

## Send an email

Inject `IEmailService` and pass a typed email:

```csharp
await emailService.SendAsync(user.Email, new WelcomeEmail(user.FullName, user.Email), cancellationToken);
```

`SendAsync` talks to SMTP right away (typically 100–500 ms) and throws if the server is down. So decide
where it runs:

| Email | Send it | Why |
| --- | --- | --- |
| A side effect: welcome email, notifications, receipts | **From a background job** (the default) | The request doesn't wait for SMTP, and a failing SMTP server is retried instead of losing the email |
| The operation itself, when the user waits for it: a login code | Directly, in the request | The client must know if it failed: translate the exception into `ExternalServiceException` (`502`) |

The welcome email is the example of the first kind: `UserService` only enqueues it, and
`Features/Users/Jobs/SendWelcomeEmailJob.cs` loads the user and sends it:

```csharp
// in the service: one line, no SMTP in the request
jobs.Enqueue<SendWelcomeEmailJob>(job => job.ExecuteAsync(user.Id, CancellationToken.None));

// the job (queue "emails", retried up to 5 times)
[Queue(JobQueues.Emails)]
[AutomaticRetry(Attempts = 5)]
public sealed class SendWelcomeEmailJob(AppDbContext db, IEmailService emailService, ...)
{
    public async Task ExecuteAsync(int userId, CancellationToken cancellationToken) { ... }
}
```

See [Background jobs](background-jobs.md).

## Add a new email

Example: a password reset email in the `Auth` feature.

**1. The model** — `Features/Auth/Emails/PasswordResetEmail.cs`:

```csharp
using DotnetApiTemplate.Api.Infrastructure.Email;

namespace DotnetApiTemplate.Api.Features.Auth.Emails;

public sealed class PasswordResetEmail(string fullName, string resetUrl, int validMinutes) : EmailTemplate
{
    public string FullName { get; } = fullName;

    public string ResetUrl { get; } = resetUrl;

    public int ValidMinutes { get; } = validMinutes;

    public override string Subject => "Reset your password";
}
```

Every public property becomes a template variable in snake_case: `FullName` → `full_name`.

**2. The HTML body** — `Features/Auth/Emails/PasswordResetEmail.html.scriban` (same name as the class):

```html
<h1>Hi {{ full_name }},</h1>
<p>We received a request to reset your {{ app_name }} password.</p>
<p><a class="button" href="{{ reset_url }}">Reset password</a></p>
<p class="muted">The link is valid for {{ valid_minutes }} minutes. If you didn't ask for this, ignore this email.</p>
```

Write only the body: the layout adds the header, footer and page structure.

**3. Optional plain text** — `PasswordResetEmail.txt.scriban`:

```text
Hi {{ full_name }},

Reset your {{ app_name }} password: {{ reset_url }}

The link is valid for {{ valid_minutes }} minutes.
```

Without this file, the text part is generated from the HTML (links become `text (url)`). Write one when
the generated text reads badly.

**4. Send it:**

```csharp
await emailService.SendAsync(user.Email, new PasswordResetEmail(user.FullName, url, 30), cancellationToken);
```

Nothing to register: templates are embedded automatically (the `.csproj` embeds every `*.scriban`).

## Template reference

| Variable | Value |
| --- | --- |
| your properties | snake_case: `{{ full_name }}`, `{{ reset_url }}` |
| `{{ subject }}` | the email's `Subject` |
| `{{ app_name }}` | `EMAIL_FROM_NAME` |
| `{{ year }}` | current year (UTC) |
| `{{ content }}` | layout only: the rendered body |

Scriban syntax you'll need (full docs: [scriban/docs/language.md](https://github.com/scriban/scriban/blob/master/doc/language.md)):

```html
{{ if items.size > 0 }}
  <ul>
  {{ for item in items }}
    <li>{{ item.name }} — {{ item.price | math.format "0.00" }}</li>
  {{ end }}
  </ul>
{{ else }}
  <p>No items.</p>
{{ end }}

{{ created_at | date.to_string "%d %b %Y" }}
{{ full_name | string.upcase }}
{{ description ?? "No description" }}
{{ # a comment, not rendered }}
```

That's a loop over a collection, number and date formatting, a string function, a fallback for `null`,
and a comment. Collections keep snake_case: a `List<OrderLine> Items` property is `items`, and each
line's `Price` is `item.price`.

### Escaping

**Every `{{ value }}` is HTML-escaped** (`<b>` → `&lt;b&gt;`), so user data can't inject markup or links.
For HTML you control, opt out with `raw`:

```html
{{ footer_html | raw }}
```

Never use `raw` on anything a user typed. The plain-text template is not escaped (it isn't HTML).

## Layout and styles

- `Layout/_layout.html.scriban` is the frame for every email: logo/app name, content, footer. Change it to
  brand all emails at once.
- `Layout/email.css` holds all styles. Use **classes** (`button`, `muted`) in templates; PreMailer.Net
  copies the CSS into `style=""` attributes when rendering.
- Email HTML is not web HTML: tables for layout, simple properties (colors, padding, fonts), no flexbox/grid,
  no JavaScript. Mailpit's **HTML Check** tab shows what each client supports.

In Development, templates and CSS are read from disk on every render, so edits show up in the next email
without a restart. Elsewhere they're embedded in the assembly and cached.

## Rules

- **Templates are code.** Never render a template that comes from a user or the database.
- Template file names must be **unique across the project** (they're embedded by file name).
- Keep logic in C#: compute values in the model; templates only display them.
- Subjects are plain text; don't put HTML in `Subject`.

## Production SMTP

Set these in the production environment:

```bash
EMAIL_HOST=smtp.your-provider.com
EMAIL_PORT=587                  # 587 = STARTTLS (EMAIL_ENABLE_SSL=true), 465 = implicit TLS
EMAIL_ENABLE_SSL=true
EMAIL_USERNAME=...
EMAIL_PASSWORD=...
EMAIL_FROM=no-reply@your-domain.com
EMAIL_FROM_NAME=Your App
```

| Provider | Host | Port | Username |
| --- | --- | --- | --- |
| Amazon SES | `email-smtp.<region>.amazonaws.com` | 587 | SMTP credentials from the SES console |
| SendGrid | `smtp.sendgrid.net` | 587 | `apikey` (password: the API key) |
| Mailgun | `smtp.mailgun.org` | 587 | the domain's SMTP login |
| Microsoft 365 | `smtp.office365.com` | 587 | the mailbox address |

Set up SPF, DKIM and DMARC for the sending domain, or emails land in spam.

## Testing

- **Unit tests** render real templates with `ScribanEmailRenderer` (see `ScribanEmailRendererTests`):
  assert the subject, content, escaping and inlined styles.
- **Services** get `FakeEmailService` (unit tests), which records `(to, email)` pairs: assert that the right
  email was sent.
- **Integration tests** replace the sender with `FakeEmailSender` (`factory.Emails.Sent`): emails are
  rendered for real, but captured instead of delivered.
