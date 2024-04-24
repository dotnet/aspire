using System.Net;

namespace AspireStarterApplication.1.Tests;

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
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireStarterApplication.1_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
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
