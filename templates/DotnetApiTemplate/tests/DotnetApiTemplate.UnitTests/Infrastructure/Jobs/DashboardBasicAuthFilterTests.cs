using System.Text;
using DotnetApiTemplate.Api.Infrastructure.Jobs;

namespace DotnetApiTemplate.UnitTests.Infrastructure.Jobs;

public sealed class DashboardBasicAuthFilterTests
{
    private readonly DashboardBasicAuthFilter _sut = new("admin", "s3cret-password");

    private static string Basic(string credentials) => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

    [Fact]
    public void IsAuthorized_WithCorrectCredentials_ReturnsTrue() =>
        Assert.True(_sut.IsAuthorized(Basic("admin:s3cret-password")));

    [Theory]
    [InlineData("admin:wrong")]
    [InlineData("other:s3cret-password")]
    [InlineData("admin:s3cret-password-longer")]
    [InlineData("admin")]
    public void IsAuthorized_WithWrongCredentials_ReturnsFalse(string credentials) =>
        Assert.False(_sut.IsAuthorized(Basic(credentials)));

    [Theory]
    [InlineData("")]
    [InlineData("Bearer abc")]
    [InlineData("Basic")]
    [InlineData("Basic not-base64!!")]
    public void IsAuthorized_WithMissingOrMalformedHeader_ReturnsFalse(string header) =>
        Assert.False(_sut.IsAuthorized(header));
}
