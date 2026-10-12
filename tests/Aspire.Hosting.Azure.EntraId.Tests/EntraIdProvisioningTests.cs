// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Azure environment resources are experimental.
#pragma warning disable ASPIREPIPELINES001 // Verifies provisioning annotations on existing resources.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.EntraId.Provisioning;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.Tests.Utils;
using Azure.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.EntraId.Tests;

public class EntraIdProvisioningTests(ITestOutputHelper outputHelper)
{
    private const string TenantId = "aaaabbbb-0000-cccc-1111-dddd2222eeee";
    private const string ClientId = "00001111-aaaa-2222-bbbb-3333cccc4444";

    [Fact]
    public async Task AddEntraIdApplication_GeneratesGraphBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web");

        Assert.IsAssignableFrom<AzureProvisioningResource>(entra.Resource);
        Assert.NotNull(entra.Resource.ProvisioningTaskCompletionSource);
        Assert.False(entra.Resource.ProvisioningTaskCompletionSource.Task.IsCompleted);
        Assert.Equal("entra-web", entra.Resource.Parameters["applicationName"]);
        Assert.Null(entra.Resource.Parameters["principalId"]);
        Assert.Equal("AzureADMyOrg", entra.Resource.Parameters["signInAudience"]);
        Assert.Single(builder.Resources.OfType<AzureEnvironmentResource>());

        await Verify(entra.Resource.GetBicepTemplateString(), "bicep");
    }

    [Fact]
    public async Task ConfigureInfrastructure_CustomizesGraphResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web")
            .ConfigureInfrastructure(infrastructure =>
            {
                var application = Assert.Single(infrastructure.GetProvisionableResources().OfType<MicrosoftGraphApplication>());
                var servicePrincipal = Assert.Single(infrastructure.GetProvisionableResources().OfType<MicrosoftGraphServicePrincipal>());
                application.DisplayName = "Contoso's web application";
                application.RedirectUris = ["https://localhost:7123/signin-oidc", "https://localhost:7123/auth/callback"];
                application.EnableIdTokenIssuance = true;
                Assert.Equal("application.appId", servicePrincipal.AppId.Compile().ToString());
                infrastructure.Add(new ProvisioningOutput("applicationDisplayName", typeof(string))
                {
                    Value = application.DisplayName.ToBicepExpression()
                });
            });

        await Verify(entra.Resource.GetBicepTemplateString(), "bicep");
    }

    [Fact]
    public void GetBicepTemplateFile_IsIdempotent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web")
            .WithParameter("redirectUris", ["https://localhost:7123/signin-oidc"]);
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        using var first = entra.Resource.GetBicepTemplateFile(workspace.Path);
        var firstContents = File.ReadAllText(first.Path);
        using var second = entra.Resource.GetBicepTemplateFile(workspace.Path);
        var secondContents = File.ReadAllText(second.Path);

        Assert.Equal(firstContents, secondContents);
        Assert.Equal(firstContents, entra.Resource.GetBicepTemplateString());
    }

    [Fact]
    public async Task AddEntraIdApplication_PublishesManifestWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var entra = builder.AddEntraIdApplication("entra-web")
            .WithSignInAudience(EntraIdSignInAudience.AzureADMultipleOrgs)
            .WithParameter("redirectUris", ["https://localhost:7123/signin-oidc"]);

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var manifest = await ManifestUtils.GetManifest(entra.Resource, workspace.Path);
        await Verify(manifest.ToJsonString(), "json")
            .AppendContentAsFile(entra.Resource.GetBicepTemplateString(), "bicep");
    }

    [Fact]
    public async Task Publish_SignInOnlyRegistration_RequiresExplicitIdTokenIssuance()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputHelper, workspace.Path);
        var web = builder.AddEntraIdApplication("entra-web")
            .WithParameter("redirectUris", [
                "https://localhost:7123/signin-oidc",
                "https://localhost:7123/signout-callback-oidc"]);
        web.Resource.Parameters["enableIdTokenIssuance"] = true;

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var environment = model.Resources.OfType<AzureEnvironmentResource>().Single();
        var options = app.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>().Value;
        await using var step = await new TestPipelineActivityReporter(outputHelper).CreateStepAsync("Publishing sign-in-only registration");
        var context = new AzurePublishingContext(
            workspace.Path, options, app.Services, NullLogger<AzurePublishingContext>.Instance, step);

        await context.WriteModelAsync(model, environment);

        await Verify(await File.ReadAllTextAsync(Path.Combine(workspace.Path, "main.bicep")), "bicep")
            .AppendContentAsFile(await File.ReadAllTextAsync(Path.Combine(workspace.Path, "entra-web", "entra-web.bicep")), "bicep");
    }

    [Fact]
    public async Task Publish_CreatesSelfContainedGraphModulesAndOutputReferences()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, outputHelper, workspace.Path);
        var api = builder.AddEntraIdApplication("entra-api");
        var web = builder.AddEntraIdApplication("entra-web")
            .WithSignInAudience(EntraIdSignInAudience.AzureADMultipleOrgs)
            .WithParameter("redirectUris", ["https://localhost:7123/signin-oidc"]);
        builder.AddEntraIdApplication("existing").AsExistingApplication(tenantId: TenantId, clientId: ClientId);
        builder.AddResource(new ProjectResource("web")).WithReference(web);
        builder.AddResource(new ProjectResource("api")).WithReference(api);
        builder.AddBicepTemplateString("consumer",
            """
            param location string = resourceGroup().location
            param tenantId string
            param clientId string

            output authority string = 'https://login.microsoftonline.com/${tenantId}/v2.0'
            output applicationClientId string = clientId
            """)
            .WithParameter("tenantId", web.Resource.TenantIdOutputReference)
            .WithParameter("clientId", web.Resource.ClientIdOutputReference);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var environment = model.Resources.OfType<AzureEnvironmentResource>().Single();
        var options = app.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>().Value;
        await using var step = await new TestPipelineActivityReporter(outputHelper).CreateStepAsync("Publishing Entra registrations");
        var context = new AzurePublishingContext(
            workspace.Path, options, app.Services, NullLogger<AzurePublishingContext>.Instance, step);

        await context.WriteModelAsync(model, environment);

        var mainBicep = await File.ReadAllTextAsync(Path.Combine(workspace.Path, "main.bicep"));
        var apiBicep = await File.ReadAllTextAsync(Path.Combine(workspace.Path, "entra-api", "entra-api.bicep"));
        var webBicep = await File.ReadAllTextAsync(Path.Combine(workspace.Path, "entra-web", "entra-web.bicep"));
        var consumerBicep = await File.ReadAllTextAsync(Path.Combine(workspace.Path, "consumer", "consumer.bicep"));
        Assert.Equal(apiBicep, webBicep);
        Assert.Equal(new[] { "consumer", "entra-api", "entra-web" }, Directory.GetDirectories(workspace.Path).Select(Path.GetFileName).Order());
        await Verify(mainBicep, "bicep")
            .AppendContentAsFile(webBicep, "bicep")
            .AppendContentAsFile(consumerBicep, "bicep");
    }

    [Theory]
    [InlineData(EntraIdSignInAudience.AzureADMyOrg)]
    [InlineData(EntraIdSignInAudience.AzureADMultipleOrgs)]
    [InlineData(EntraIdSignInAudience.AzureADandPersonalMicrosoftAccount)]
    [InlineData(EntraIdSignInAudience.PersonalMicrosoftAccount)]
    public void WithSignInAudience_UpdatesProvisioningParameter(EntraIdSignInAudience audience)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web");
        entra.WithSignInAudience(audience);

        Assert.Equal(audience.ToString(), entra.Resource.Parameters["signInAudience"]);
    }

    [Fact]
    public async Task AsExistingApplication_DoesNotPublishOrAwaitProvisioning()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        Assert.True(entra.Resource.IsExistingApplication);
        Assert.Null(entra.Resource.ProvisioningTaskCompletionSource);
        Assert.Empty(entra.Resource.Annotations.OfType<PipelineStepAnnotation>());
        Assert.Empty(entra.Resource.Annotations.OfType<PipelineConfigurationAnnotation>());
        Assert.Null(await ManifestUtils.GetManifestOrNull(entra.Resource));
    }

    [Fact]
    public void AsExistingApplication_PreservesAppHostPipelineAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var customization = new PipelineConfigurationAnnotation(_ =>
        {
        });
        var entra = builder.AddEntraIdApplication("entra-web")
            .WithAnnotation(customization)
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        Assert.Same(customization, Assert.Single(entra.Resource.Annotations.OfType<PipelineConfigurationAnnotation>()));
    }

    [Theory]
    [InlineData(DistributedApplicationOperation.Run)]
    [InlineData(DistributedApplicationOperation.Publish)]
    public void AsExisting_RejectsArmResourceIdentity(DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);
        var entra = builder.AddEntraIdApplication("entra-web")
            .AsExisting(builder.AddParameter("application-name"), builder.AddParameter("resource-group"));

        var exception = Assert.Throws<InvalidOperationException>(entra.Resource.GetBicepTemplateString);
        Assert.Equal(
            "Entra ID applications are identified by tenant ID and client ID, not by an Azure resource name or resource group. " +
            "Use AsExistingApplication(tenantId, clientId) to reference an existing registration without provisioning it.",
            exception.Message);
    }

    [Fact]
    public void GetBicepTemplate_RejectsInvalidSignInAudience()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web");
        entra.Resource.SignInAudience = (EntraIdSignInAudience)42;

        var exception = Assert.Throws<InvalidOperationException>(entra.Resource.GetBicepTemplateString);
        Assert.Equal("'42' is not a valid EntraIdSignInAudience value.", exception.Message);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("ABC123", "CN=Contoso")]
    public void GetBicepTemplate_RejectsInvalidCredentialBeforeProvisioning(string? thumbprint, string? distinguishedName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web")
            .WithCredential(new EntraIdStoreCertificateCredential
            {
                StorePath = "CurrentUser/My",
                Thumbprint = thumbprint,
                DistinguishedName = distinguishedName
            });

        var exception = Assert.Throws<InvalidOperationException>(entra.Resource.GetBicepTemplateString);

        Assert.Equal(thumbprint is null
            ? "Either Thumbprint or DistinguishedName must be set on EntraIdStoreCertificateCredential."
            : "Only one of Thumbprint or DistinguishedName can be set on EntraIdStoreCertificateCredential.",
            exception.Message);
    }

    [Theory]
    [InlineData(DistributedApplicationOperation.Run, EntraIdSignInAudience.AzureADMyOrg, null)]
    [InlineData(DistributedApplicationOperation.Publish, EntraIdSignInAudience.AzureADMyOrg, null)]
    [InlineData(DistributedApplicationOperation.Run, EntraIdSignInAudience.AzureADMultipleOrgs, "organizations")]
    [InlineData(DistributedApplicationOperation.Publish, EntraIdSignInAudience.AzureADMultipleOrgs, "organizations")]
    [InlineData(DistributedApplicationOperation.Run, EntraIdSignInAudience.AzureADandPersonalMicrosoftAccount, "common")]
    [InlineData(DistributedApplicationOperation.Publish, EntraIdSignInAudience.AzureADandPersonalMicrosoftAccount, "common")]
    [InlineData(DistributedApplicationOperation.Run, EntraIdSignInAudience.PersonalMicrosoftAccount, "consumers")]
    [InlineData(DistributedApplicationOperation.Publish, EntraIdSignInAudience.PersonalMicrosoftAccount, "consumers")]
    public async Task WithReference_CreatedApplication_UsesOutputs(
        DistributedApplicationOperation operation,
        EntraIdSignInAudience audience,
        string? signInTenant)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web").WithSignInAudience(audience);
        entra.Resource.Outputs["tenantId"] = TenantId;
        entra.Resource.Outputs["clientId"] = ClientId;
        entra.Resource.ProvisioningTaskCompletionSource!.SetResult();

        var project = builder.AddResource(new ProjectResource("web")).WithReference(entra);
        var container = builder.AddContainer("worker", "myimage").WithReference(entra, connectionName: "ENTRA");

        var tenantId = operation == DistributedApplicationOperation.Run ? TenantId : "{entra-web.outputs.tenantId}";
        var clientId = operation == DistributedApplicationOperation.Run ? ClientId : "{entra-web.outputs.clientId}";
        var expectedProject = new Dictionary<string, string>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__TenantId"] = signInTenant ?? tenantId,
            ["AzureAd__ClientId"] = clientId
        };
        var expectedContainer = new Dictionary<string, string>
        {
            ["ENTRA_INSTANCE"] = "https://login.microsoftonline.com/",
            ["ENTRA_TENANT_ID"] = signInTenant ?? tenantId,
            ["ENTRA_CLIENT_ID"] = clientId
        };
        if (signInTenant is not null)
        {
            expectedProject["AzureAd__AppHomeTenantId"] = tenantId;
            expectedContainer["ENTRA_APP_HOME_TENANT_ID"] = tenantId;
        }

        Assert.Equal(expectedProject, await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            project.Resource, operation, TestServiceProvider.Instance));
        Assert.Equal(expectedContainer, await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, operation, TestServiceProvider.Instance));
    }

    [Fact]
    public async Task ClientIdOutputReference_WaitsForProvisioning()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var entra = builder.AddEntraIdApplication("entra-web");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var value = entra.Resource.ClientIdOutputReference.GetValueAsync(cts.Token).AsTask();

        Assert.False(value.IsCompleted);
        entra.Resource.Outputs["clientId"] = ClientId;
        entra.Resource.ProvisioningTaskCompletionSource!.SetResult();

        Assert.Equal(ClientId, await value);
    }
}
