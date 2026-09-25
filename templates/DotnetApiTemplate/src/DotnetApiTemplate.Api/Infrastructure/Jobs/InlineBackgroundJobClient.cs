using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

/// <summary>
/// The job client when JOBS_ENABLED=false: <c>jobs.Enqueue&lt;T&gt;(...)</c> runs the job immediately, in the calling
/// request, in its own DI scope. Nothing is stored and nothing is retried: a failing job is logged (the request
/// still succeeds, like with a real queue) and then it's gone. Delayed jobs are rejected instead of running early.
/// Features don't change: they use <see cref="IBackgroundJobClient"/> in both modes.
/// </summary>
internal sealed class InlineBackgroundJobClient(IServiceScopeFactory scopes, ILogger<InlineBackgroundJobClient> logger)
    : IBackgroundJobClient
{
    public string Create(Job job, IState state)
    {
        if (state is ScheduledState)
        {
            throw new NotSupportedException(
                $"Delayed job {job.Type.Name}.{job.Method.Name} can't run with JOBS_ENABLED=false: " +
                "set JOBS_ENABLED=true (Hangfire), or run the work directly.");
        }

        // Enqueued, and continuations (their parent already ran inline): run now.
        var jobId = $"inline-{Guid.NewGuid():N}";
        Run(job, jobId);
        return jobId;
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => false;   // nothing is stored

    private void Run(Job job, string jobId)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var instance = job.Method.IsStatic ? null : ActivatorUtilities.CreateInstance(scope.ServiceProvider, job.Type);
            var args = job.Args.Select(arg => arg is CancellationToken ? CancellationToken.None : arg).ToArray();

            if (job.Method.Invoke(instance, args) is Task task)
            {
                // Enqueue is synchronous by design, so the async job is awaited here (no sync context in ASP.NET Core).
                task.GetAwaiter().GetResult();
            }

            logger.LogInformation("Ran job {JobType}.{JobMethod} inline ({JobId}); JOBS_ENABLED=false", job.Type.Name, job.Method.Name, jobId);
        }
        catch (Exception ex)
        {
            var error = ex is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : ex;
            logger.LogError(error, "Inline job {JobType}.{JobMethod} failed and won't be retried (JOBS_ENABLED=false)",
                job.Type.Name, job.Method.Name);
        }
    }
}
