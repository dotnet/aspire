namespace Aspire_StarterApplication._1.Tests;

/// <summary>
/// Contains tests for web resources.
/// </summary>
#if (TestFramework == "MSTest")
[TestClass]
#endif
public class WebTests
{
    /// <summary>
    /// Tests if the root web resource returns an OK status code.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var app = await appHost.BuildAsync().ConfigureAwait(true);
#pragma warning restore CA2007
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync().ConfigureAwait(true);

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var response = await httpClient.GetAsync(new Uri("/", UriKind.Relative)).ConfigureAwait(true);

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
