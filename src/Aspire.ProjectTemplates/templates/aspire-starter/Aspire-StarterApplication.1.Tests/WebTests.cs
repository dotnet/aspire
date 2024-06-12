namespace Aspire_StarterApplication._1.Tests;

#if (TestFramework == "MSTest")
[TestClass]
#endif
#if (TestFramework == "xUnit.net")
public class WebTests(ITestOutputHelper output)
#else
public class WebTests
#endif
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
        appHost.Services.AddLogging(logging => logging.AddXunit(output));
#endif
        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        using var waitForResourceCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running, waitForResourceCts.Token);
        var httpClient = app.CreateHttpClient("webfrontend");
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
