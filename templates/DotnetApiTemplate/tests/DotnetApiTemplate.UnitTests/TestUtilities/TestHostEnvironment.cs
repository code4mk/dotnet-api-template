using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DotnetApiTemplate.UnitTests.TestUtilities;

internal sealed class TestHostEnvironment(string environmentName = "Production") : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
