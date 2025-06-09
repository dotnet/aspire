// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerAppsTests
{
    [Fact]
    public async Task AddContainerAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddDockerfileWithAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var directory = Directory.CreateTempSubdirectory(".aspire-test");

        // Contents of the Dockerfile are not important for this test
        File.WriteAllText(Path.Combine(directory.FullName, "Dockerfile"), "");

        builder.AddDockerfile("api", directory.FullName);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddContainerAppEnvironmentAddsDeploymentTargetWithContainerAppToProjectResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.IsType<IComputeResource>(Assert.Single(model.GetProjectResources()), exactMatch: false);

        var target = container.GetDeploymentTargetAnnotation();

        Assert.NotNull(target);
        Assert.Same(env.Resource, target.ComputeEnvironment);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddExecutableResourceWithPublishAsDockerFileWithAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var infra = builder.AddAzureContainerAppEnvironment("infra");

        var env = builder.AddParameter("env");

        builder.AddExecutable("api", "node.exe", Environment.CurrentDirectory)
               .PublishAsDockerFile()
               .PublishAsAzureContainerApp((infra, app) =>
               {
                   app.Template.Containers[0].Value!.Env.Add(new ContainerAppEnvironmentVariable()
                   {
                       Name = "Hello",
                       Value = env.AsProvisioningParameter(infra)
                   });
               });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.IsType<IComputeResource>(Assert.Single(model.GetContainerResources()), exactMatch: false);

        var target = container.GetDeploymentTargetAnnotation();

        Assert.NotNull(target);
        Assert.Same(infra.Resource, target.ComputeEnvironment);

        var resource = target.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task CanTweakContainerAppEnvironmentUsingPublishAsContainerAppOnExecutable()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        builder.AddExecutable("api", "node.exe", Environment.CurrentDirectory)
               .PublishAsDockerFile();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        var target = container.GetDeploymentTargetAnnotation();

        Assert.Same(env.Resource, target?.ComputeEnvironment);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddContainerAppsInfrastructureWithParameterReference()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var value = builder.AddParameter("value");
        var minReplicas = builder.AddParameter("minReplicas");

        builder.AddContainer("api", "myimage")
               .PublishAsAzureContainerApp((module, c) =>
               {
                   var val = new ContainerAppEnvironmentVariable()
                   {
                       Name = "Parameter",
                       Value = value.AsProvisioningParameter(module)
                   };

                   c.Template.Containers[0].Value!.Env.Add(val);
                   c.Template.Scale.MinReplicas = minReplicas.AsProvisioningParameter(module);
               });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AddContainerAppsEntrypointAndArgs()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
               .WithEntrypoint("/bin/sh")
               .WithArgs("my", "args with space");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ProjectWithManyReferenceTypes()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var db = builder.AddAzureCosmosDB("mydb");
        db.AddCosmosDatabase("cosmosdb", databaseName: "db");

        // Postgres uses secret outputs + a literal connection string
        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").WithPasswordAuthentication().AddDatabase("db");

        var rawCs = builder.AddConnectionString("cs");

        var blob = builder.AddAzureStorage("storage").AddBlobs("blobs");

        // Secret parameters (_ isn't supported and will be replaced by -)
        var secretValue = builder.AddParameter("value0", "x", secret: true);

        // Normal parameters
        var value = builder.AddParameter("value1", "y");

        var project = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithHttpsEndpoint()
            .WithHttpEndpoint(name: "internal")
            .WithReference(db)
            .WithReference(blob)
            .WithReference(pgdb)
            .WithEnvironment("SecretVal", secretValue)
            .WithEnvironment("secret_value_1", secretValue)
            .WithEnvironment("Value", value)
            .WithEnvironment("CS", rawCs);

        project.WithEnvironment(context =>
        {
            var httpEp = project.GetEndpoint("http");
            var httpsEp = project.GetEndpoint("https");
            var internalEp = project.GetEndpoint("internal");

            context.EnvironmentVariables["HTTP_EP"] = project.GetEndpoint("http");
            context.EnvironmentVariables["HTTPS_EP"] = project.GetEndpoint("https");
            context.EnvironmentVariables["INTERNAL_EP"] = project.GetEndpoint("internal");
            context.EnvironmentVariables["TARGET_PORT"] = httpEp.Property(EndpointProperty.TargetPort);
            context.EnvironmentVariables["PORT"] = httpEp.Property(EndpointProperty.Port);
            context.EnvironmentVariables["HOST"] = httpEp.Property(EndpointProperty.Host);
            context.EnvironmentVariables["HOSTANDPORT"] = httpEp.Property(EndpointProperty.HostAndPort);
            context.EnvironmentVariables["SCHEME"] = httpEp.Property(EndpointProperty.Scheme);
            context.EnvironmentVariables["INTERNAL_HOSTANDPORT"] = internalEp.Property(EndpointProperty.HostAndPort);
        });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var proj = Assert.Single(model.GetProjectResources());
        var identityName = $"{proj.Name}-identity";
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == identityName);

        proj.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(identityManifest.ToString(), "json")
              .AppendContentAsFile(identityBicep, "bicep");
    }

    [Fact]
    public async Task ProjectWithManyReferenceTypesAndContainerAppEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("cae");

        var db = builder.AddAzureCosmosDB("mydb");
        db.AddCosmosDatabase("cosmosdb", databaseName: "db");

        // Postgres uses secret outputs + a literal connection string
        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").WithPasswordAuthentication().AddDatabase("db");

        var rawCs = builder.AddConnectionString("cs");

        var blob = builder.AddAzureStorage("storage").AddBlobs("blobs");

        // Secret parameters (_ isn't supported and will be replaced by -)
        var secretValue = builder.AddParameter("value0", "x", secret: true);

        // Normal parameters
        var value = builder.AddParameter("value1", "y");

        var project = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithHttpsEndpoint()
            .WithHttpEndpoint(name: "internal")
            .WithReference(db)
            .WithReference(blob)
            .WithReference(pgdb)
            .WithEnvironment("SecretVal", secretValue)
            .WithEnvironment("secret_value_1", secretValue)
            .WithEnvironment("Value", value)
            .WithEnvironment("CS", rawCs);

        project.WithEnvironment(context =>
        {
            var httpEp = project.GetEndpoint("http");
            var httpsEp = project.GetEndpoint("https");
            var internalEp = project.GetEndpoint("internal");

            context.EnvironmentVariables["HTTP_EP"] = project.GetEndpoint("http");
            context.EnvironmentVariables["HTTPS_EP"] = project.GetEndpoint("https");
            context.EnvironmentVariables["INTERNAL_EP"] = project.GetEndpoint("internal");
            context.EnvironmentVariables["TARGET_PORT"] = httpEp.Property(EndpointProperty.TargetPort);
            context.EnvironmentVariables["PORT"] = httpEp.Property(EndpointProperty.Port);
            context.EnvironmentVariables["HOST"] = httpEp.Property(EndpointProperty.Host);
            context.EnvironmentVariables["HOSTANDPORT"] = httpEp.Property(EndpointProperty.HostAndPort);
            context.EnvironmentVariables["SCHEME"] = httpEp.Property(EndpointProperty.Scheme);
            context.EnvironmentVariables["INTERNAL_HOSTANDPORT"] = internalEp.Property(EndpointProperty.HostAndPort);
        });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var proj = Assert.Single(model.GetProjectResources());
        var identityName = $"{proj.Name}-identity";
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == identityName);

        proj.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(identityManifest.ToString(), "json")
              .AppendContentAsFile(identityBicep, "bicep");
    }

    [Fact]
    public async Task AzureContainerAppsBicepGenerationIsIdempotent()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var secret = builder.AddParameter("secret", secret: true);
        var kv = builder.AddAzureKeyVault("kv");

        builder.AddContainer("api", "myimage")
               .WithEnvironment("TOP_SECRET", secret)
                .WithEnvironment("TOP_SECRET2", kv.Resource.GetSecret("secret"));

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        _ = await GetManifestWithBicep(resource);
        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task AzureContainerAppsMapsPortsForBaitAndSwitchResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddExecutable("api", "node", ".")
            .PublishAsDockerFile()
            .WithHttpEndpoint(env: "PORT");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public void MultipleCallsToAddAzureContainerAppEnvironmentThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env1");
        var ex = Assert.Throws<NotSupportedException>(() => builder.AddAzureContainerAppEnvironment("env2"));

        Assert.Equal("Only one container app environment is supported at this time. Found: env1", ex.Message);
    }

    [Fact]
    public async Task MultipleAzureContainerAppEnvironmentThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env1");

        builder.Resources.Add(new AzureContainerAppEnvironmentResource("env2", infra => { }));

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("Multiple container app environments are not supported.", ex.Message);
    }

    [Fact]
    public async Task PublishAsContainerAppInfluencesContainerAppDefinition()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");
        builder.AddContainer("api", "myimage")
            .PublishAsAzureContainerApp((module, c) =>
            {
                Assert.Contains(c, module.GetProvisionableResources());

                c.Template.Scale.MinReplicas = 0;
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ConfigureCustomDomainMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain = builder.AddParameter("customDomain");
        var certificateName = builder.AddParameter("certificateName");

        builder.AddAzureContainerAppEnvironment("env");
        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 1111)
            .PublishAsAzureContainerApp((module, c) =>
            {
                c.ConfigureCustomDomain(customDomain, certificateName);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ConfigureDuplicateCustomDomainMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain = builder.AddParameter("customDomain");
        var initialCertificateName = builder.AddParameter("initialCertificateName");
        var expectedCertificateName = builder.AddParameter("expectedCertificateName");

        builder.AddAzureContainerAppEnvironment("env");
        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 1111)
            .PublishAsAzureContainerApp((module, c) =>
            {
                c.ConfigureCustomDomain(customDomain, initialCertificateName);
                c.ConfigureCustomDomain(customDomain, expectedCertificateName);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ConfigureMultipleCustomDomainsMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain1 = builder.AddParameter("customDomain1");
        var certificateName1 = builder.AddParameter("certificateName1");

        var customDomain2 = builder.AddParameter("customDomain2");
        var certificateName2 = builder.AddParameter("certificateName2");

        builder.AddAzureContainerAppEnvironment("env");
        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 1111)
            .PublishAsAzureContainerApp((module, c) =>
            {
                c.ConfigureCustomDomain(customDomain1, certificateName1);
                c.ConfigureCustomDomain(customDomain2, certificateName2);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task VolumesAndBindMountsAreTranslation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithVolume("vol1", "/path1")
            .WithVolume("vol2", "/path2")
            .WithBindMount("bind1", "/path3");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task KeyVaultReferenceHandling()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var db = builder.AddAzureCosmosDB("mydb").WithAccessKeyAuthentication();
        db.AddCosmosDatabase("db");

        builder.AddContainer("api", "image")
            .WithReference(db);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task SecretOutputsThrowNotSupportedExceptionWithContainerAppEnvironmentResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("cae");

        var resource = builder.AddAzureInfrastructure("resourceWithSecret", infra =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var kvNameParam = new ProvisioningParameter(AzureBicepResource.KnownParameters.KeyVaultName, typeof(string));
#pragma warning restore CS0618 // Type or member is obsolete
            infra.Add(kvNameParam);

            var kv = KeyVaultService.FromExisting("kv");
            kv.Name = kvNameParam;
            infra.Add(kv);

            var secret = new KeyVaultSecret("kvs")
            {
                Name = "myconnection",
                Properties = new()
                {
                    Value = "top secret"
                },
                Parent = kv,
            };

            infra.Add(secret);
        });

        var container = builder.AddContainer("api", "image")
            .WithEnvironment(context =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                context.EnvironmentVariables["secret0"] = resource.GetSecretOutput("myconnection");
#pragma warning restore CS0618 // Type or member is obsolete
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var target = container.Resource.GetDeploymentTargetAnnotation()?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(target);

        var ex = Assert.Throws<NotSupportedException>(() => target.GetBicepTemplateFile());

        Assert.Equal("Automatic Key vault generation is not supported in this environment. Please create a key vault resource directly.", ex.Message);
    }

    [Fact]
    public async Task CanCustomizeWithProvisioningBuildOptions()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.Services.Configure<AzureProvisioningOptions>(options => options.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new MyResourceNamePropertyResolver()));
        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api1", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (_, bicep) = await GetManifestWithBicep(resource);

        await Verify(bicep, "bicep");
    }

    private sealed class MyResourceNamePropertyResolver : DynamicResourceNamePropertyResolver
    {
        public override void ResolveProperties(ProvisionableConstruct construct, ProvisioningBuildOptions options)
        {
            if (construct is ContainerApp app)
            {
                app.Name = app.Name.Value + "-my";
            }

            base.ResolveProperties(construct, options);
        }
    }

    [Fact]
    public async Task ExternalEndpointBecomesIngress()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task FirstHttpEndpointBecomesIngress()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(name: "one", targetPort: 8080)
            .WithHttpEndpoint(name: "two", targetPort: 8081);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task EndpointWithHttp2SetsTransportToH2()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint()
            .WithEndpoint("http", e => e.Transport = "http2")
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ProjectUsesTheTargetPortAsADefaultPortForFirstHttpEndpoint()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddProject<Project>("api", launchProfileName: null)
               .WithHttpEndpoint()
               .WithHttpsEndpoint();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task RoleAssignmentsWithAsExisting()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var storageName = builder.AddParameter("storageName");
        var storageRG = builder.AddParameter("storageRG");

        var storage = builder.AddAzureStorage("storage")
            .PublishAsExisting(storageName, storageRG);
        var blobs = storage.AddBlobs("blobs");

        builder.AddProject<Project>("api", launchProfileName: null)
               .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataReader);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-identity");
        var projRolesStorage = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-storage");

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);
        var (rolesStorageManifest, rolesStorageBicep) = await GetManifestWithBicep(projRolesStorage);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(rolesStorageManifest.ToString(), "json")
              .AppendContentAsFile(rolesStorageBicep, "bicep")
              .AppendContentAsFile(identityManifest.ToString(), "json")
              .AppendContentAsFile(identityBicep, "bicep");
    }

    [Fact]
    public async Task RoleAssignmentsWithAsExistingCosmosDB()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var cosmosName = builder.AddParameter("cosmosName");
        var cosmosRG = builder.AddParameter("cosmosRG");

        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .PublishAsExisting(cosmosName, cosmosRG);

        builder.AddProject<Project>("api", launchProfileName: null)
               .WithReference(cosmos);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-identity");
        var projRolesStorage = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-cosmos");

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);
        var (rolesCosmosManifest, rolesCosmosBicep) = await GetManifestWithBicep(projRolesStorage);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(rolesCosmosManifest.ToString(), "json")
              .AppendContentAsFile(rolesCosmosBicep, "bicep")
              .AppendContentAsFile(identityManifest.ToString(), "json")
              .AppendContentAsFile(identityBicep, "bicep");
    }

    [Fact]
    public async Task RoleAssignmentsWithAsExistingRedis()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var redis = builder.AddAzureRedis("redis")
            .PublishAsExisting("myredis", "myRG");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(redis);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-identity");
        var projRolesStorage = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-redis");

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);
        var (rolesRedisManifest, rolesRedisBicep) = await GetManifestWithBicep(projRolesStorage);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(rolesRedisManifest.ToString(), "json")
              .AppendContentAsFile(rolesRedisBicep, "bicep")
              .AppendContentAsFile(identityManifest.ToString(), "json")
              .AppendContentAsFile(identityBicep, "bicep");
    }

    [Fact]
    public async Task NonTcpHttpOrUdpSchemeThrows()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithEndpoint(scheme: "foo");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("The endpoint(s) 'foo' specify an unsupported scheme. The supported schemes are 'http', 'https', and 'tcp'.", ex.Message);
    }

    [Fact]
    public async Task MultipleExternalEndpointsAreNotSupported()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(name: "ep1")
            .WithHttpEndpoint(name: "ep2")
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("Multiple external endpoints are not supported", ex.Message);
    }

    [Fact]
    public async Task ExternalNonHttpEndpointsAreNotSupported()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithEndpoint("ep1", e => e.IsExternal = true);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("External non-HTTP(s) endpoints are not supported", ex.Message);
    }

    [Fact]
    public async Task HttpAndTcpEndpointsCannotHaveTheSameTargetPort()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 80)
            .WithEndpoint(targetPort: 80);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("HTTP(s) and TCP endpoints cannot be mixed", ex.Message);
    }

    [Fact]
    public async Task DefaultHttpIngressMustUsePort80()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(port: 8081);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal($"The endpoint 'http' is an http endpoint and must use port 80", ex.Message);
    }

    [Fact]
    public async Task DefaultHttpsIngressMustUsePort443()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpsEndpoint(port: 8081);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal($"The endpoint 'https' is an https endpoint and must use port 443", ex.Message);
    }

    [Fact]
    public async Task AddContainerAppEnvironmentDoesNotAddEnvironmentResourceInRunMode()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        builder.AddAzureContainerAppEnvironment("env");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Empty(model.Resources.OfType<AzureContainerAppEnvironmentResource>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddContainerAppEnvironmentAddsEnvironmentResource(bool useAzdNaming)
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        if (useAzdNaming)
        {
            env.WithAzdResourceNaming();
        }

        var pg = builder.AddAzurePostgresFlexibleServer("pg")
                        .WithPasswordAuthentication()
                        .AddDatabase("db");

        builder.AddContainer("cache", "redis")
               .WithVolume("App.da-ta", "/data")
               .WithReference(pg);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var environment = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

        var (manifest, bicep) = await GetManifestWithBicep(environment);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    // see https://github.com/dotnet/aspire/issues/8381 for more information on this scenario
    // Azure SqlServer needs an admin when it is first provisioned. To supply this, we use the
    // principalId from the Azure Container App Environment.
    [Fact]
    public async Task AddContainerAppEnvironmentWorksWithSqlServer()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var sql = builder.AddAzureSqlServer("sql");
        var db = sql.AddDatabase("db").WithDefaultAzureSku();

        builder.AddContainer("cache", "redis")
               .WithReference(db);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(sql.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ContainerAppEnvironmentWithCustomRegistry()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Create a custom registry
        var registry = builder.AddAzureContainerRegistry("customregistry");

        // Create a container app environment and associate it with the custom registry
        builder.AddAzureContainerAppEnvironment("env")
            .WithAzureContainerRegistry(registry);

        // Add a container that will use the environment
        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify environment resource exists
        var environment = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

        // Verify project resource exists
        var project = Assert.Single(model.GetProjectResources());

        // Get the bicep for the environment
        var (envManifest, envBicep) = await GetManifestWithBicep(environment);

        // Verify container has correct deployment target
        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var projectResource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(projectResource);

        // Get the bicep for the container
        var (containerManifest, containerBicep) = await GetManifestWithBicep(projectResource);

        // Verify the Azure Container Registry resource manifest and bicep
        var containerRegistry = Assert.Single(model.Resources.OfType<AzureContainerRegistryResource>());
        var (registryManifest, registryBicep) = await GetManifestWithBicep(containerRegistry);

        await Verify(envManifest.ToString(), "json")
              .AppendContentAsFile(envBicep, "bicep")
              .AppendContentAsFile(containerManifest.ToString(), "json")
              .AppendContentAsFile(containerBicep, "bicep")
              .AppendContentAsFile(registryManifest.ToString(), "json")
              .AppendContentAsFile(registryBicep, "bicep");
    }

    [Fact]
    public async Task ContainerAppEnvironmentWithCustomWorkspace()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Create a custom Log Analytics Workspace
        var workspace = builder.AddAzureLogAnalyticsWorkspace("customworkspace");

        // Create a container app environment and associate it with the custom workspace
        builder.AddAzureContainerAppEnvironment("env")
            .WithAzureLogAnalyticsWorkspace(workspace);

        // Add a container that will use the environment
        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify environment resource exists
        var environment = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

        // Verify project resource exists
        var project = Assert.Single(model.GetProjectResources());

        // Get the bicep for the environment
        var (envManifest, envBicep) = await GetManifestWithBicep(environment);

        // Verify container has correct deployment target
        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var projectResource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(projectResource);

        // Get the bicep for the container
        var (containerManifest, containerBicep) = await GetManifestWithBicep(projectResource);

        // Verify the Azure Log Analytics Workspace resource manifest and bicep
        var logAnalyticsWorkspace = Assert.Single(model.Resources.OfType<AzureLogAnalyticsWorkspaceResource>());
        var (workspaceManifest, workspaceBicep) = await GetManifestWithBicep(logAnalyticsWorkspace);

        await Verify(envManifest.ToString(), "json")
              .AppendContentAsFile(envBicep, "bicep")
              .AppendContentAsFile(containerManifest.ToString(), "json")
              .AppendContentAsFile(containerBicep, "bicep")
              .AppendContentAsFile(workspaceManifest.ToString(), "json")
              .AppendContentAsFile(workspaceBicep, "bicep");
    }

    [Fact]
    public async Task CanReferenceContainerAppEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        var azResource = builder.AddAzureInfrastructure("infra", infra =>
        {
            var managedEnvironment = (ContainerAppManagedEnvironment)env.Resource.AddAsExistingResource(infra);

            infra.Add(new ProvisioningOutput("id", typeof(string))
            {
                Value = managedEnvironment.Id
            });
        });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(azResource.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ContainerAppEnvironmentWithDashboardEnabled()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env")
               .WithDashboard(true);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerAppEnvResource = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

        var (manifest, bicep) = await GetManifestWithBicep(containerAppEnvResource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task ContainerAppEnvironmentWithDashboardDisabled()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env")
               .WithDashboard(false);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerAppEnvResource = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

        var (manifest, bicep) = await GetManifestWithBicep(containerAppEnvResource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task UnknownManifestExpressionProviderIsHandledWithAllocateParameter()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var customProvider = new CustomManifestExpressionProvider();
        
        builder.AddContainer("api", "myimage")
               .WithEnvironment(context =>
               {
                   context.EnvironmentVariables["CUSTOM_VALUE"] = customProvider;
               })
               .PublishAsAzureContainerApp((_, _) => { });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);
        var resource = target?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public void AzureContainerAppEnvironmentImplementsIAzureComputeEnvironmentResource()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var env = builder.AddAzureContainerAppEnvironment("env");

        Assert.IsAssignableFrom<IAzureComputeEnvironmentResource>(env.Resource);
        Assert.IsAssignableFrom<IComputeEnvironmentResource>(env.Resource);
    }

    private static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource) =>
        AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }

    [Fact]
    public async Task ContainerAppWithUppercaseName_ShouldUseLowercaseInManifest()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        // This is the problematic case - uppercase name "WebFrontEnd"
        builder.AddContainer("WebFrontEnd", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    private sealed class CustomManifestExpressionProvider : IManifestExpressionProvider
    {
        public string ValueExpression => "{customValue}";
    }
}
