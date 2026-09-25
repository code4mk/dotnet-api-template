using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

namespace DotnetApiTemplate.UnitTests.Infrastructure.Jobs;

public sealed class JobsRegistrationTests
{
    private static IServiceCollection Register(string? role, string? jobsEnabled = null)
    {
        var values = new Dictionary<string, string?>();
        if (role is not null) values["APP_ROLE"] = role;
        if (jobsEnabled is not null) values["JOBS_ENABLED"] = jobsEnabled;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
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
    [InlineData("all")]
    [InlineData("api")]
    [InlineData("worker")]
    public void JobsDisabled_UsesTheInlineClient_AndNoHangfire(string role)
    {
        var services = Register(role, jobsEnabled: "false");

        var client = Assert.Single(services, d => d.ServiceType == typeof(IBackgroundJobClient));
        Assert.Equal(typeof(InlineBackgroundJobClient), client.ImplementationType);
        Assert.False(HasJobServer(services));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(JobStorage));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void JobsEnabled_IsTheDefault() => Assert.True(new JobsSettings().Enabled);

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
