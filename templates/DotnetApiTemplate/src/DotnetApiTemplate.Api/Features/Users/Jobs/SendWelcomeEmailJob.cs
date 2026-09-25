using Hangfire;
using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Features.Users.Emails;
using DotnetApiTemplate.Api.Infrastructure.Email;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

namespace DotnetApiTemplate.Api.Features.Users.Jobs;

/// <summary>
/// Sends the welcome email after sign-up, in the background: sign-up doesn't wait for SMTP, and a failing
/// SMTP server is retried instead of losing the email.
/// Enqueue: <c>jobs.Enqueue&lt;SendWelcomeEmailJob&gt;(job =&gt; job.ExecuteAsync(user.Id, CancellationToken.None));</c>
/// </summary>
[Queue(JobQueues.Emails)]
[AutomaticRetry(Attempts = 5)]   // retried with increasing delays, then shown as failed in the dashboard
public sealed class SendWelcomeEmailJob(AppDbContext db, IEmailService emailService, ILogger<SendWelcomeEmailJob> logger)
{
    /// <summary>Takes only the user id: the job loads current data when it runs.</summary>
    public async Task ExecuteAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("User {UserId} no longer exists; welcome email skipped", userId);
            return;   // nothing to do: don't fail (and retry) a job that can never succeed
        }

        await emailService.SendAsync(user.Email, new WelcomeEmail(user.FullName, user.Email), cancellationToken);
    }
}
