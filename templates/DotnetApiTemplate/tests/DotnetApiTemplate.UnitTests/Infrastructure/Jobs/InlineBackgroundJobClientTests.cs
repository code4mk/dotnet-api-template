using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

namespace DotnetApiTemplate.UnitTests.Infrastructure.Jobs;

public sealed class InlineBackgroundJobClientTests
{
    public sealed class Recorder
    {
        public List<int> Runs { get; } = [];
    }

    public sealed class RecordingJob(Recorder recorder)
    {
        public async Task ExecuteAsync(int value, CancellationToken cancellationToken)
        {
            await Task.Yield();
            recorder.Runs.Add(value);
        }

        public Task FailAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("SMTP down");
    }

    private readonly Recorder _recorder = new();
    private readonly InlineBackgroundJobClient _sut;

    public InlineBackgroundJobClientTests()
    {
        var services = new ServiceCollection().AddSingleton(_recorder).BuildServiceProvider();
        _sut = new InlineBackgroundJobClient(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<InlineBackgroundJobClient>.Instance);
    }

    [Fact]
    public void Enqueue_RunsTheJobImmediately_WithDependenciesFromDI()
    {
        _sut.Enqueue<RecordingJob>(job => job.ExecuteAsync(42, CancellationToken.None));

        Assert.Equal([42], _recorder.Runs);
    }

    [Fact]
    public void Enqueue_WhenTheJobFails_LogsInsteadOfThrowing()
    {
        var id = _sut.Enqueue<RecordingJob>(job => job.FailAsync(CancellationToken.None));   // must not throw into the request

        Assert.StartsWith("inline-", id);
    }

    [Fact]
    public void Schedule_IsRejected_InsteadOfRunningEarly()
    {
        var error = Assert.Throws<NotSupportedException>(() =>
            _sut.Schedule<RecordingJob>(job => job.ExecuteAsync(1, CancellationToken.None), TimeSpan.FromHours(24)));

        Assert.Contains("JOBS_ENABLED", error.Message);
        Assert.Empty(_recorder.Runs);
    }
}
