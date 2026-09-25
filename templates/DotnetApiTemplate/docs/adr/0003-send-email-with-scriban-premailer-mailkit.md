# ADR-0003: Send Email with Scriban Templates, PreMailer.Net and MailKit

We will build emails as typed C# classes rendered from Scriban templates, inline their CSS with PreMailer.Net and
send them over SMTP with MailKit.

| Field | Value |
| --- | --- |
| Status | Accepted |
| Date | 2026-09-24 |
| Scope | Every email the API sends (transactional: sign-up, password reset, notifications) |
| Code | `Infrastructure/Email/`, templates next to each feature (`Features/<Feature>/Emails/`) |

## Context

Almost every API sends email. We need one way to do it that works the same in every project and every
environment.

Forces:

- **Templates people can edit.** Email content changes more often than code; it should be readable markup, not
  string concatenation in C#.
- **Safe with user data.** Names, addresses and other user input end up in emails. They must never be able to
  inject HTML, links or scripts.
- **It must look right in real inboxes.** Gmail, Outlook and many mobile clients ignore `<style>` blocks and
  modern CSS; styles have to be inline.
- **Plain text too.** Spam filters and some clients expect a `text/plain` part next to the HTML.
- **Provider-neutral.** Projects use different providers (SES, SendGrid, Mailgun, Microsoft 365, a company
  SMTP server). Switching provider must be a configuration change.
- **Supported, maintained libraries.** `System.Net.Mail.SmtpClient` is documented by Microsoft as not
  recommended for new development; Microsoft points to MailKit instead.
- **Easy local development.** Developers must see the real rendered email without sending it to anyone.

## Options considered

| Option | Verdict |
| --- | --- |
| **Scriban** templates (Liquid-like, fast, no build step) | ✅ Chosen. Simple syntax, supports loops, conditions and formatting; templates are plain text files |
| Razor (RazorLight, or .NET's `HtmlRenderer` with components) | Strong typing and auto-escaping, but heavier to write and to render outside MVC; overkill for email bodies |
| Fluid / Handlebars.Net | Comparable; Scriban has the larger user base in .NET and more built-in functions |
| Provider-hosted templates (e.g. SendGrid dynamic templates) | Ties content to one provider, not reviewable in code, hard to test |
| **MailKit** for SMTP | ✅ Chosen. The de facto standard .NET mail library, recommended by Microsoft, full TLS support |
| `System.Net.Mail.SmtpClient` | Not recommended for new development by Microsoft |
| Provider SDKs (SendGrid, SES) | Lock-in; SMTP works with all of them |
| **PreMailer.Net** for CSS inlining | ✅ Chosen. Mature, does one job: moves CSS from a stylesheet into `style=""` attributes |
| Hand-written inline styles | Error-prone and impossible to change consistently |

## Decision

We render and send email with this pipeline:

```text
typed email class (model + subject)
  → Scriban template (every value HTML-escaped) → shared layout
  → PreMailer.Net inlines Layout/email.css
  → plain-text part (optional .txt template, or generated from the HTML)
  → MailKit over SMTP   (or only logged when EMAIL_HOST is empty)
```

1. **Typed emails.** Each email is a class deriving from `EmailTemplate` (properties are the template model,
   `Subject` the subject). Features send with `IEmailService.SendAsync(to, email)`.
2. **Templates next to their feature**, named after the class (`WelcomeEmail.html.scriban`, optional
   `.txt.scriban`), embedded in the assembly so they always ship with the build. In Development they are read
   from disk, so edits show up without a restart.
3. **Automatic HTML escaping.** Every `{{ value }}` is HTML-encoded by the renderer; trusted HTML needs an
   explicit `{{ value | raw }}`. Scriban doesn't escape by default, so this is enforced in code
   (`HtmlEscapingTemplateContext`), not left to template authors.
4. **Templates are code.** Only developer-written templates are rendered, never templates from users or the
   database. (Scriban's past security advisories concern rendering untrusted templates; the package is pinned to
   a patched version.)
5. **One layout and one stylesheet** (`Infrastructure/Email/Layout/`) for every email; templates use classes,
   PreMailer.Net inlines them.
6. **SMTP only, configured by environment** (`EMAIL_*`): implicit TLS on port 465, STARTTLS on others. An empty
   `EMAIL_HOST` logs emails instead of sending them.
7. **Mailpit in development** catches every email in a local inbox (Docker Compose).
8. **Emails that are side effects are sent from a background job** (see [ADR-0004](0004-run-background-jobs-with-hangfire-and-postgresql.md)),
   so a slow or failing SMTP server neither delays nor fails the request, and failures are retried.

## Consequences

**Positive**

- Emails are reviewable, testable code: rendering (content, escaping, inlining) is covered by unit tests.
- User data can't inject markup, by default.
- Consistent look in real clients without hand-written inline CSS.
- Any SMTP provider works; switching is configuration.
- Developers see real emails locally in Mailpit; nothing reaches real inboxes.

**Negative**

- Three libraries to keep up to date (Scriban, PreMailer.Net, MailKit).
- Scriban templates aren't type-checked: a misspelled `{{ ful_name }}` renders empty instead of failing the build.
- Template file names must be unique across the project (they're embedded by name).
- Email HTML has to follow email-client limits (tables, simple CSS), which is a different skill from web HTML.

**Risks and mitigations**

| Risk | Mitigation |
| --- | --- |
| Someone uses `raw` on user input | Code review; `raw` is the only way to bypass escaping and easy to search for |
| A Scriban vulnerability | Templates are never user-supplied; package version pinned; `dotnet list package --vulnerable` in reviews |
| Template variable typos | Unit tests that render each email and assert its content |
| Provider-specific features needed (tracking, bulk sending) | Add a provider SDK behind `IEmailSender` for that project; the rendering stays the same |
| Emails land in spam | Configure SPF, DKIM and DMARC for the sending domain (documented) |

## References

- [Developer guide: Email](../development/email.md)
- [Scriban](https://github.com/scriban/scriban) and its [language reference](https://github.com/scriban/scriban/blob/master/doc/language.md)
- [PreMailer.Net](https://github.com/milkshakesoftware/PreMailer.Net)
- [MailKit](https://github.com/jstedfast/MailKit)
- [SmtpClient remarks (Microsoft)](https://learn.microsoft.com/dotnet/api/system.net.mail.smtpclient#remarks): not recommended for new development
- [Mailpit](https://mailpit.axllent.org/)
