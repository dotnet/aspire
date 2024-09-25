namespace Aspire.Tests._1;

[TestClass]
public class IntegrationTest1
{
    // Instructions:
    // 1. Add a project reference to the target AppHost project, e.g.:
    //
    //    <ItemGroup>
    //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
    //    </ItemGroup>
    //
    // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
    //
    /// <summary>
    /// Tests if the root web resource returns an OK status code.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    //    [TestMethod]
    //    public async Task GetWebResourceRootReturnsOkStatusCode()
    //    {
    //        // Arrange
    //        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspireApp_AppHost>();
    //        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    //        {
    //            clientBuilder.AddStandardResilienceHandler();
    //        });
    //#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
    //        await using var app = await appHost.BuildAsync().ConfigureAwait(true);
    //#pragma warning restore CA2007
    //        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
    //        await app.StartAsync().ConfigureAwait(true);
    //
    //        // Act
    //        var httpClient = app.CreateHttpClient("webfrontend");
    //        await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
    //        var response = await httpClient.GetAsync(new Uri("/", UriKind.Relative)).ConfigureAwait(true);
    //
    //        // Assert
    //        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    //    }
}
