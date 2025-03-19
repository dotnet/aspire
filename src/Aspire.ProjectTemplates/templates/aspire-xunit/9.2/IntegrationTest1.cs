using Microsoft.Extensions.Logging;

namespace Aspire.Tests._1.Tests;

public class IntegrationTest1
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    // Instructions:
    // 1. Add a project reference to the target AppHost project, e.g.:
    //
    //    <ItemGroup>
    //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
    //    </ItemGroup>
    //
    // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
    //
    // [Fact]
    // public async Task GetWebResourceRootReturnsOkStatusCode()
    // {
    //     // Arrange
    //     var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspireApp_AppHost>();
    //     appHost.Services.AddLogging(logging =>
    //     {
    //         logging.SetMinimumLevel(LogLevel.Debug);
    //         // Override the logging filters from the app's configuration
    //         logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
    //         logging.AddFilter("Aspire.", LogLevel.Debug);
    //         // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
    //     });
    //     appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    //     {
    //         clientBuilder.AddStandardResilienceHandler();
    //     });
    //
    //     await using var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
    //     await app.StartAsync().WaitAsync(DefaultTimeout);
    //
    //     // Act
    //     var httpClient = app.CreateHttpClient("webfrontend");
    //     await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend").WaitAsync(DefaultTimeout);
    //     var response = await httpClient.GetAsync("/");
    //
    //     // Assert
    //     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    // }
}
