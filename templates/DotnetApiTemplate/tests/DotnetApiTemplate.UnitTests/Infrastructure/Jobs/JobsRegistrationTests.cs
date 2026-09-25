using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

namespace DotnetApiTemplate.UnitTests.Infrastructure.Jobs;

public sealed class JobsRegistrationTests
{
    private static IServiceCollection Register(string? role)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(role is null ? [] : [new KeyValuePair<string, string?>("APP_ROLE", role)])
            .Build();
        return new ServiceCollection().AddJobs(configuration);
    }

    private static bool HasJobServer(IServiceCollection services) =>
        services.Any(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(JobsStartupService));

    [Theory]
    [InlineData(null)]      // default: all
    [InlineData("all")]
    [InlineData("worker")]
    [InlineData("WORKER")]
    public void RolesThatRunJobs_RegisterTheJobServer(string? role) => Assert.True(HasJobServer(Register(role)));

    [Fact]
    public void ApiRole_OnlyEnqueues()
    {
        var services = Register("api");

        Assert.False(HasJobServer(services));
        Assert.Contains(services, d => d.ServiceType == typeof(IBackgroundJobClient));
    }

    [Theory]
    [InlineData("all", true, true)]
    [InlineData("api", true, false)]
    [InlineData("worker", false, true)]
    public void AppSettings_Role_DecidesWhatRuns(string role, bool api, bool jobs)
    {
        var settings = new AppSettings { Role = role };

        Assert.Equal(api, settings.RunsApi);
        Assert.Equal(jobs, settings.RunsJobs);
    }

    [Fact]
    public void JobsSettings_ParsesQueuesInPriorityOrder()
    {
        var settings = new JobsSettings { QueueList = " Emails , default,,reports " };

        Assert.Equal(["emails", "default", "reports"], settings.Queues);
    }
}
