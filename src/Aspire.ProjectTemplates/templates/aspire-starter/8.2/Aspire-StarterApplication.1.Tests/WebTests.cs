namespace Aspire_StarterApplication._1.Tests;

#if (TestFx == "MSTest")
[TestClass]
#endif
public class WebTests
{
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
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
#if (TestFx == "xUnit.net")
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
#endif

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
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
