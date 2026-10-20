// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable ASPIREACAEXPRESS001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureFrontDoorPrivateLinkTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkConvertsOriginsToPrivateLinkOrigins()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        var web = builder.AddProject<Project>("web", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        frontDoor.WithOrigin(api).WithOrigin(web);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(frontDoor.Resource);

        await Verify(bicep, "bicep");
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkDisablesPublicNetworkAccessOnTheEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(environment.Resource);

        await Verify(bicep, "bicep");
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkOrderDoesNotMatter()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        var frontDoor = builder.AddAzureFrontDoor("frontdoor")
            .WithOrigin(api);
        builder.AddAzureContainerAppEnvironment("env")
            .WithAzureFrontDoorPrivateLink(frontDoor);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(frontDoor.Resource);

        Assert.Contains("name: 'Premium_AzureFrontDoor'", bicep);
        Assert.Contains("groupId: 'managedEnvironments'", bicep);
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkUsesExplicitLocation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor, "westus2");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(frontDoor.Resource);

        await Verify(bicep, "bicep");
    }

    [Theory]
    [InlineData("westus2")]
    [InlineData("West US 2")]
    [InlineData("WESTUS2")]
    public async Task WithAzureFrontDoorPrivateLinkNormalizesTheLocation(string location)
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor, location);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(frontDoor.Resource);

        Assert.Contains("privateLinkLocation: 'westus2'", bicep);
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkResolvesSameEnvironmentReferencesToInternalHosts()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        var web = builder.AddProject<Project>("web", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints()
            .WithReference(api)
            .WithEnvironment("API_HOST", api.GetEndpoint("https").Property(EndpointProperty.Host));
        frontDoor.WithOrigin(api).WithOrigin(web);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(GetContainerApp(web.Resource));

        await Verify(bicep, "bicep");
    }

    [Fact]
    public async Task SameEnvironmentReferencesUsePublicHostsWithoutPrivateLink()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        builder.AddAzureContainerAppEnvironment("env");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        var web = builder.AddProject<Project>("web", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints()
            .WithReference(api);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(GetContainerApp(web.Resource));

        Assert.DoesNotContain(".internal.", bicep);
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkProbesOverHttps()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints()
            .WithHttpProbe(ProbeType.Liveness, "/health", endpointName: "http");
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, bicep) = await GetManifestWithBicep(frontDoor.Resource);

        Assert.Contains("probePath: '/health'", bicep);
        Assert.Contains("probeProtocol: 'Https'", bicep);
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkLeavesOriginsInOtherEnvironmentsPublic()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var privateEnvironment = builder.AddAzureContainerAppEnvironment("private-env");
        var publicEnvironment = builder.AddAzureContainerAppEnvironment("public-env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        privateEnvironment.WithAzureFrontDoorPrivateLink(frontDoor);

        var privateApi = builder.AddProject<Project>("private-api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints()
            .WithComputeEnvironment(privateEnvironment);
        var publicApi = builder.AddProject<Project>("public-api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints()
            .WithComputeEnvironment(publicEnvironment);
        frontDoor.WithOrigin(privateApi).WithOrigin(publicApi);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, frontDoorBicep) = await GetManifestWithBicep(frontDoor.Resource);
        var (_, publicEnvironmentBicep) = await GetManifestWithBicep(publicEnvironment.Resource);

        Assert.Equal(1, CountOccurrences(frontDoorBicep, "sharedPrivateLinkResource:"));
        Assert.DoesNotContain("publicNetworkAccess", publicEnvironmentBicep);
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkConnectsEachFrontDoorToTheEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var first = builder.AddAzureFrontDoor("first");
        var second = builder.AddAzureFrontDoor("second");
        environment.WithAzureFrontDoorPrivateLink(first).WithAzureFrontDoorPrivateLink(second);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        var web = builder.AddProject<Project>("web", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        first.WithOrigin(api);
        second.WithOrigin(web);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (_, firstBicep) = await GetManifestWithBicep(first.Resource);
        var (_, secondBicep) = await GetManifestWithBicep(second.Resource);

        Assert.Contains("requestMessage: 'Azure Front Door first requests access to Azure Container Apps environment env.'", firstBicep);
        Assert.Contains("requestMessage: 'Azure Front Door second requests access to Azure Container Apps environment env.'", secondBicep);
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkThrowsWhenAnotherFrontDoorHasAnOriginInTheEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var connected = builder.AddAzureFrontDoor("connected");
        var unconnected = builder.AddAzureFrontDoor("unconnected");
        environment.WithAzureFrontDoorPrivateLink(connected);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        var web = builder.AddProject<Project>("web", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        connected.WithOrigin(api);
        unconnected.WithOrigin(web);

        using var app = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Contains("Origin 'web' of Azure Front Door 'unconnected'", exception.ToString());
        Assert.Contains("WithAzureFrontDoorPrivateLink", exception.ToString());
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkThrowsWhenNoOriginRunsInTheEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var privateEnvironment = builder.AddAzureContainerAppEnvironment("private-env");
        var publicEnvironment = builder.AddAzureContainerAppEnvironment("public-env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        privateEnvironment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints()
            .WithComputeEnvironment(publicEnvironment);
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Contains("none of its origins run in that environment", exception.ToString());
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkThrowsForExpressEnvironments()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env").AsExpress();
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Contains("Express environment 'env' cannot be the target of a private link", exception.ToString());
    }

    [Fact]
    public async Task WithAzureFrontDoorPrivateLinkThrowsForExistingEnvironments()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env")
            .PublishAsExisting("existing-env", "existing-rg");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint()
            .WithExternalHttpEndpoints();
        frontDoor.WithOrigin(api);

        using var app = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Contains("is marked as existing but is also the target of a private link", exception.ToString());
    }

    [Fact]
    public void WithAzureFrontDoorPrivateLinkIsIdempotentForTheSameLocation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");

        environment.WithAzureFrontDoorPrivateLink(frontDoor, "westus2");
        environment.WithAzureFrontDoorPrivateLink(frontDoor, "West US 2");

        Assert.Single(frontDoor.Resource.Annotations.OfType<AzureFrontDoorPrivateLinkAnnotation>());
    }

    [Fact]
    public void WithAzureFrontDoorPrivateLinkThrowsWhenCalledAgainWithADifferentLocation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");
        environment.WithAzureFrontDoorPrivateLink(frontDoor, "westus2");

        var exception = Assert.Throws<InvalidOperationException>(() => environment.WithAzureFrontDoorPrivateLink(frontDoor, "eastus2"));

        Assert.Contains("already connected to Azure Front Door 'frontdoor' with a different Private Link location", exception.Message);
    }

    [Fact]
    public void WithAzureFrontDoorPrivateLinkDoesNothingInRunMode()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");

        var result = environment.WithAzureFrontDoorPrivateLink(frontDoor);

        Assert.Same(environment, result);
        Assert.Empty(environment.Resource.Annotations.OfType<PrivateEndpointTargetAnnotation>());
        Assert.Empty(frontDoor.Resource.Annotations.OfType<AzureFrontDoorPrivateLinkAnnotation>());
    }

    [Fact]
    public void WithAzureFrontDoorPrivateLinkValidatesArguments()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment("env");
        var frontDoor = builder.AddAzureFrontDoor("frontdoor");

        Assert.Throws<ArgumentNullException>(() => ((IResourceBuilder<AzureContainerAppEnvironmentResource>)null!).WithAzureFrontDoorPrivateLink(frontDoor));
        Assert.Throws<ArgumentNullException>(() => environment.WithAzureFrontDoorPrivateLink(null!));
        Assert.Throws<ArgumentException>(() => environment.WithAzureFrontDoorPrivateLink(frontDoor, ""));
        Assert.Throws<ArgumentException>(() => environment.WithAzureFrontDoorPrivateLink(frontDoor, " "));
    }

    [Fact]
    public void RequestMessageStaysWithinTheFrontDoorLimit()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, testOutputHelper);

        var environment = builder.AddAzureContainerAppEnvironment(new string('e', 55));
        var frontDoor = builder.AddAzureFrontDoor(new string('f', 55));
        environment.WithAzureFrontDoorPrivateLink(frontDoor);

        var connection = Assert.Single(frontDoor.Resource.Annotations.OfType<AzureFrontDoorPrivateLinkAnnotation>());

        Assert.Equal(140, connection.RequestMessage.Length);
    }

    private static IResource GetContainerApp(IResource resource)
    {
        var target = resource.GetDeploymentTargetAnnotation();
        Assert.NotNull(target);
        return Assert.IsAssignableFrom<AzureProvisioningResource>(target.DeploymentTarget);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
