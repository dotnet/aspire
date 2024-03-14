using System.Net;

namespace AspireStarterApplication.1.Tests;

public class WebTests
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Program>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        var result = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
