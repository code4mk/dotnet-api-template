using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace DotnetApiTemplate.UnitTests.TestUtilities;

/// <summary>Records enqueued jobs instead of storing them: assert on <see cref="Jobs"/>.</summary>
internal sealed class FakeBackgroundJobClient : IBackgroundJobClient
{
    public List<Job> Jobs { get; } = [];

    public string Create(Job job, IState state)
    {
        Jobs.Add(job);
        return Jobs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;
}
