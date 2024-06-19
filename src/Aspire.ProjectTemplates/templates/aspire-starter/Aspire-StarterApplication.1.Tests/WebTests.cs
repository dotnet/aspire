namespace Aspire_StarterApplication._1.Tests;

#if (TestFramework == "MSTest")
[TestClass]
#endif
public class WebTests
{
#if (TestFramework == "MSTest")
    [TestMethod]
#elif (TestFramework == "NUnit")
    [Test]
#elif (TestFramework == "xUnit.net")
    [Fact]
#endif
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.GeneratedClassNamePrefix_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
#if (TestFramework == "xUnit.net")
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
#endif

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var webfrontend = appModel.GetProjectResources().First(p => string.Equals(p.Name, "webfrontend", StringComparison.OrdinalIgnoreCase));
        await resourceNotificationService.WaitForResourceAsync(webfrontend.Name, KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var httpClient = app.CreateHttpClient(webfrontend.Name);
        var response = await httpClient.GetAsync("/");

        // Assert
#if (TestFramework == "MSTest")
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
#elif (TestFramework == "NUnit")
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
#elif (TestFramework == "xUnit.net")
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
#endif
    }
}
