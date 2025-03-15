// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzurePublisherTests
{
    [Fact]
    public async Task PublishAsync_GeneratesMainBicep()
    {
        using var tempDirectory = new TempDirectory();
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new AzurePublisherOptions { OutputPath = tempDir.Path });
        var provisionerOptions = Options.Create(new AzureProvisioningOptions());

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // The azure publisher is not tied to azure container apps but this is 
        // a good way to test the end to end scenario
        builder.AddAzureContainerAppsInfrastructure();

        var storageSku = builder.AddParameter("storageSku", "Standard_LRS", publishValueAsDefault: true);
        var description = builder.AddParameter("skuDescription", "The sku is ", publishValueAsDefault: true);
        var skuDescriptionExpr = ReferenceExpression.Create($"{description} {storageSku}");

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

        var publisher = new AzurePublisher("azure",
            options,
            provisionerOptions,
            NullLogger<AzurePublisher>.Instance);

        await publisher.PublishAsync(model, default);

        Assert.True(File.Exists(Path.Combine(tempDir.Path, "main.bicep")));

        var content = File.ReadAllText(Path.Combine(tempDir.Path, "main.bicep"));

        Assert.Equal(
            """
            targetScope = 'subscription'

            param environmentName string
            
            param location string
            
            param principalId string
            
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
            
            module pg 'pg/pg.bicep' = {
              name: 'pg'
              scope: rg
              params: {
                location: location
                principalId: ''
                principalType: ''
                principalName: ''
              }
            }
            
            module account 'account/account.bicep' = {
              name: 'account'
              scope: rg
              params: {
                location: location
                principalType: ''
                principalId: ''
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
            
            module fe_roles 'fe-roles/fe-roles.bicep' = {
              name: 'fe-roles'
              scope: rg
              params: {
                location: location
                storage_outputs_name: storage.outputs.name
              }
            }
            
            output account_connectionString string = account.outputs.connectionString
            
            output fe_roles_id string = fe_roles.outputs.id
            
            output fe_roles_clientId string = fe_roles.outputs.clientId
            
            output storage_blobEndpoint string = storage.outputs.blobEndpoint
            """,
                content, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

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
