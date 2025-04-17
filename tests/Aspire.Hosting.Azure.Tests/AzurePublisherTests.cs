// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzurePublisherTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_GeneratesMainBicep(bool useContext)
    {
        using var tempDirectory = new TempDirectory();
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new AzurePublisherOptions { OutputPath = tempDir.Path });
        var provisionerOptions = Options.Create(new AzureProvisioningOptions());

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // The azure publisher is not tied to azure container apps but this is 
        // a good way to test the end to end scenario
        builder.AddAzureContainerAppEnvironment("acaEnv");

        var storageSku = builder.AddParameter("storageSku", "Standard_LRS", publishValueAsDefault: true);
        var description = builder.AddParameter("skuDescription", "The sku is ", publishValueAsDefault: true);
        var skuDescriptionExpr = ReferenceExpression.Create($"{description} {storageSku}");

        var kvName = builder.AddParameter("kvName");
        var kvRg = builder.AddParameter("kvRg", "rg-shared");

        builder.AddAzureKeyVault("kv").AsExisting(kvName, kvRg);

        builder.AddAzureStorage("existing-storage").PublishAsExisting("images", "rg-shared");

        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").AddDatabase("pgdb");
        var cosmos = builder.AddAzureCosmosDB("account").AddCosmosDatabase("db");
        var blobs = builder.AddAzureStorage("storage")
                            .ConfigureInfrastructure(c =>
                            {
                                var storageAccount = c.GetProvisionableResources().OfType<StorageAccount>().FirstOrDefault();

                                storageAccount!.Sku.Name = storageSku.AsProvisioningParameter(c);

                                var output = new ProvisioningOutput("description", typeof(string))
                                {
                                    Value = skuDescriptionExpr.AsProvisioningParameter(c, "sku_description")
                                };

                                c.Add(output);
                            })
                            .AddBlobs("blobs");

        builder.AddAzureInfrastructure("mod", infra =>
        {
            // Noop
        })
        .WithParameter("pgdb", pgdb.Resource.ConnectionStringExpression);

        builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                        .WithReference(cosmos);

        builder.AddProject<TestProject>("fe", launchProfileName: null)
                        .WithEnvironment("BLOB_CONTAINER_URL", $"{blobs}/container");

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        if (useContext)
        {
            // tests the public AzurePublishingContext API
            var context = new AzurePublishingContext(
                options.CurrentValue,
                provisionerOptions.Value,
                NullLogger<AzurePublishingContext>.Instance);

            await context.WriteModelAsync(model, default);
        }
        else
        {
            // tests via the internal Publisher object
            var publisher = new AzurePublisher("azure",
                options,
                provisionerOptions,
                NullLogger<AzurePublisher>.Instance);

            await publisher.PublishAsync(model, default);
        }

        Assert.True(File.Exists(Path.Combine(tempDir.Path, "main.bicep")));

        var content = File.ReadAllText(Path.Combine(tempDir.Path, "main.bicep"));

        var expectedBicep = """
            targetScope = 'subscription'

            param environmentName string
            
            param location string
            
            param principalId string
            
            param kvRg string
            
            param kvName string
            
            param storageSku string = 'Standard_LRS'
            
            param skuDescription string = 'The sku is '
            
            var tags = {
              'aspire-env-name': environmentName
            }
            
            resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
              name: 'rg-${environmentName}'
              location: location
              tags: tags
            }
            
            module acaEnv 'acaEnv/acaEnv.bicep' = {
              name: 'acaEnv'
              scope: rg
              params: {
                location: location
                userPrincipalId: principalId
              }
            }
            
            module kv 'kv/kv.bicep' = {
              name: 'kv'
              scope: resourceGroup(kvRg)
              params: {
                location: location
                kvName: kvName
              }
            }
            
            module existing_storage 'existing-storage/existing-storage.bicep' = {
              name: 'existing-storage'
              scope: resourceGroup('rg-shared')
              params: {
                location: location
              }
            }
            
            module pg 'pg/pg.bicep' = {
              name: 'pg'
              scope: rg
              params: {
                location: location
              }
            }
            
            module account 'account/account.bicep' = {
              name: 'account'
              scope: rg
              params: {
                location: location
              }
            }
            
            module storage 'storage/storage.bicep' = {
              name: 'storage'
              scope: rg
              params: {
                location: location
                storageSku: storageSku
                sku_description: '${skuDescription} ${storageSku}'
              }
            }
            
            module mod 'mod/mod.bicep' = {
              name: 'mod'
              scope: rg
              params: {
                location: location
                pgdb: '${pg.outputs.connectionString};Database=pgdb'
              }
            }
            
            module myapp_identity 'myapp-identity/myapp-identity.bicep' = {
              name: 'myapp-identity'
              scope: rg
              params: {
                location: location
              }
            }
            
            module myapp_roles_account 'myapp-roles-account/myapp-roles-account.bicep' = {
              name: 'myapp-roles-account'
              scope: rg
              params: {
                location: location
                account_outputs_name: account.outputs.name
                principalId: myapp_identity.outputs.principalId
              }
            }
            
            module fe_identity 'fe-identity/fe-identity.bicep' = {
              name: 'fe-identity'
              scope: rg
              params: {
                location: location
              }
            }
            
            module fe_roles_storage 'fe-roles-storage/fe-roles-storage.bicep' = {
              name: 'fe-roles-storage'
              scope: rg
              params: {
                location: location
                storage_outputs_name: storage.outputs.name
                principalId: fe_identity.outputs.principalId
              }
            }
            
            output acaEnv_AZURE_CONTAINER_REGISTRY_NAME string = acaEnv.outputs.AZURE_CONTAINER_REGISTRY_NAME
            
            output acaEnv_AZURE_CONTAINER_REGISTRY_ENDPOINT string = acaEnv.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
            
            output acaEnv_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = acaEnv.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
            
            output myapp_identity_id string = myapp_identity.outputs.id
            
            output myapp_identity_clientId string = myapp_identity.outputs.clientId
            
            output account_connectionString string = account.outputs.connectionString
            
            output acaEnv_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = acaEnv.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
            
            output acaEnv_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = acaEnv.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
            
            output fe_identity_id string = fe_identity.outputs.id
            
            output fe_identity_clientId string = fe_identity.outputs.clientId
            
            output storage_blobEndpoint string = storage.outputs.blobEndpoint
            """;
        output.WriteLine(content);
        Assert.Equal(expectedBicep, content, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    private sealed class OptionsMonitor(AzurePublisherOptions options) : IOptionsMonitor<AzurePublisherOptions>
    {
        public AzurePublisherOptions Get(string? name) => options;

        public IDisposable OnChange(Action<AzurePublisherOptions, string> listener) => null!;

        public AzurePublisherOptions CurrentValue => options;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = Directory.CreateTempSubdirectory(".aspire-publisers").FullName;
        }

        public string Path { get; }
        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
