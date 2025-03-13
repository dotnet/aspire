using Microsoft.Extensions.Logging;

namespace Aspire_StarterApplication._1.Tests;

#if (TestFx == "MSTest")
[TestClass]
#endif
public class WebTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

#if (TestFx == "MSTest")
    [TestMethod]
#elif (TestFx == "NUnit")
    [Test]
#elif (TestFx == "xUnit.net")
    [Fact]
#endif
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.GeneratedClassNamePrefix_AppHost>();
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
#if (TestFx == "xUnit.net")
            // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
#endif
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
        await app.StartAsync().WaitAsync(DefaultTimeout);

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend").WaitAsync(DefaultTimeout);
        var response = await httpClient.GetAsync("/");

        // Assert
#if (TestFx == "MSTest")
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
#elif (TestFx == "NUnit")
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
#elif (TestFx == "xUnit.net")
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
#endif
    }
}
