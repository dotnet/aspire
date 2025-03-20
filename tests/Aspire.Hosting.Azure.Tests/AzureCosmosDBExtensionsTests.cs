// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBExtensionsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithGatewayPort(port);
        });

        var endpointAnnotation = cosmos.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        Assert.NotNull(endpointAnnotation);

        var actualPort = endpointAnnotation.Port;
        Assert.Equal(port, actualPort);
    }

    [Theory]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedImageTag(string imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithImageTag(imageTag);
        });

        var containerImageAnnotation = cosmos.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        var actualTag = containerImageAnnotation.Tag;
        Assert.Equal(imageTag ?? "latest", actualTag);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(12)]
    public async Task AddAzureCosmosDBWithPartitionCountCanOverrideNumberOfPartitions(int partitionCount)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(r => r.WithPartitionCount(partitionCount));
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(cosmos.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(partitionCount.ToString(CultureInfo.InvariantCulture), config["AZURE_COSMOS_EMULATOR_PARTITION_COUNT"]);
    }

    [Fact]
    public void AddAzureCosmosDBWithDataExplorer()
    {
#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        cosmos.RunAsPreviewEmulator(e => e.WithDataExplorer());

        var endpoint = cosmos.GetEndpoint("data-explorer");
        Assert.NotNull(endpoint);
        Assert.Equal(1234, endpoint.TargetPort);

        // WithDataExplorer doesn't work against the non-preview emulator
        var cosmos2 = builder.AddAzureCosmosDB("cosmos2");
        Assert.Throws<NotSupportedException>(() => cosmos2.RunAsEmulator(e => e.WithDataExplorer()));
#pragma warning restore ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
    }

    [Fact]
    public void AzureCosmosDBHasCorrectConnectionStrings_ForAccountEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        // database and container should have the same connection string as the cosmos account, for now.
        // In the future, we can add the database and container info to the connection string.
        Assert.Equal("{cosmos.outputs.connectionString}", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{cosmos.outputs.connectionString}", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{cosmos.outputs.connectionString}", container1.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AzureCosmosDBHasCorrectConnectionStrings()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        Assert.DoesNotContain(";Database=db1", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.DoesNotContain(";Database=db1;Container=container1", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Contains(";Database=db1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Contains(";Database=db1;Container=container1", container1.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AzureCosmosDBAppliesAzureFunctionsConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        var target = new Dictionary<string, object>();
        ((IResourceWithAzureFunctionsConfig)cosmos.Resource).ApplyAzureFunctionsConfiguration(target, "cosmos");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__cosmos__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__cosmos__AccountEndpoint", k),
            k => Assert.Equal("cosmos__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)db1.Resource).ApplyAzureFunctionsConfiguration(target, "db1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__db1__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__db1__AccountEndpoint", k),
            k => Assert.Equal("db1__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)container1.Resource).ApplyAzureFunctionsConfiguration(target, "container1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__container1__AccountEndpoint", k),
            k => Assert.Equal("Aspire__Microsoft__EntityFrameworkCore__Cosmos__container1__AccountEndpoint", k),
            k => Assert.Equal("container1__accountEndpoint", k));
    }

    /// <summary>
    /// Test both with and without ACA infrastructure because the role assignments
    /// are handled differently between the two. This ensures that the bicep is generated
    /// consistently regardless of the infrastructure used in RunMode.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAzureCosmosDB(bool useAcaInfrastructure)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        if (useAcaInfrastructure)
        {
            builder.AddAzureContainerAppsInfrastructure();
        }

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        builder.AddContainer("api", "myimage")
            .WithReference(cosmos);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(cosmos.Resource, skipPreparer: true);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
              name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
              location: location
              properties: {
                locations: [
                  {
                    locationName: location
                    failoverPriority: 0
                  }
                ]
                consistencyPolicy: {
                  defaultConsistencyLevel: 'Session'
                }
                databaseAccountOfferType: 'Standard'
                disableLocalAuth: true
              }
              kind: 'GlobalDocumentDB'
              tags: {
                'aspire-resource-name': 'cosmos'
              }
            }

            resource cosmos_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
              name: '00000000-0000-0000-0000-000000000002'
              parent: cosmos
            }

            resource cosmos_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
              name: guid(principalId, cosmos_roleDefinition.id, cosmos.id)
              properties: {
                principalId: principalId
                roleDefinitionId: cosmos_roleDefinition.id
                scope: cosmos.id
              }
              parent: cosmos
            }

            output connectionString string = cosmos.properties.documentEndpoint

            output name string = cosmos.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);
}
