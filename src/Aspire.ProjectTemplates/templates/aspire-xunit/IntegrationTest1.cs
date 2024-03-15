using System.Net;

namespace Aspire.Tests1;

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
    // [Fact]
    // public async Task GetWebResourceRootReturnsOkStatusCode()
    // {
    //     // Arrange
    //     var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyAspireApp_AppHost>();
    //     await using var app = await appHost.BuildAsync();
    //     await app.StartAsync();

    //     // Act
    //     var httpClient = app.CreateHttpClient("webfrontend");
    //     var response = await httpClient.GetAsync("/");

    //     // Assert
    //     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    // }
}