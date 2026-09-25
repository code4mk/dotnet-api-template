using System.Collections.Concurrent;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetApiTemplate.IntegrationTests.TestUtilities;

/// <summary>
/// Records jobs the API enqueues instead of storing them in PostgreSQL. <see cref="RunAsync"/> executes them
/// with the app's real services, like a worker would, so their effects (e.g. emails) can be asserted.
/// </summary>
public sealed class FakeBackgroundJobClient : IBackgroundJobClient
{
    public ConcurrentQueue<Job> Jobs { get; } = new();

    public string Create(Job job, IState state)
    {
        Jobs.Enqueue(job);
        return Guid.NewGuid().ToString("N");
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;

    /// <summary>Runs every recorded job matching <paramref name="predicate"/> in its own DI scope.</summary>
    public async Task RunAsync(IServiceProvider services, Func<Job, bool>? predicate = null)
    {
        foreach (var job in Jobs.Where(predicate ?? (_ => true)).ToArray())
        {
            await using var scope = services.CreateAsyncScope();
            var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, job.Type);
            var args = job.Args.Select(arg => arg is CancellationToken ? CancellationToken.None : arg).ToArray();

            if (job.Method.Invoke(instance, args) is Task task)
            {
                await task;
            }
        }
    }
}
