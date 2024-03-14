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
    // 2. Update the target AppHost project to allow this test project to see its internals, e.g.:
    //
    //    <ItemGroup>
    //        <InternalsVisibleTo Include="Aspire.Tests1" />
    //    </ItemGroup>
    //
    // 3. Uncomment the following example test:
    // 
    // [Fact]
    // public async Task GetWebResourceRootReturnsOkStatusCode()
    // {
    //     // Arrange
    //     var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Program>();
    //     await using var app = await appHost.BuildAsync();
    //     await app.StartAsync();

    //     // Act
    //     var httpClient = app.CreateHttpClient("webfrontend");
    //     var result = await httpClient.GetAsync("/");

    //     // Assert
    //     Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    // }
}