namespace Aspire.Tests._1;

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
    // [Test]
    // public async Task GetWebResourceRootReturnsOkStatusCode()
    // {
    //     // Arrange
    //     var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspireApp_AppHost>();
    //     appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    //     {
    //         clientBuilder.AddStandardResilienceHandler();
    //     });
    //     await using var app = await appHost.BuildAsync();
    //     var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
    //     await app.StartAsync();

    //     // Act
    //     var httpClient = app.CreateHttpClient("webfrontend");
    //     await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
    //     var response = await httpClient.GetAsync("/");

    //     // Assert
    //     Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    // }
}
