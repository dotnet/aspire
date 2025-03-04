// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBExtensionsTests
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
    public void AzureCosmosDBHasCorrectConnectionStrings()
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
            k => Assert.Equal("cosmos__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)db1.Resource).ApplyAzureFunctionsConfiguration(target, "db1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__db1__AccountEndpoint", k),
            k => Assert.Equal("db1__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)container1.Resource).ApplyAzureFunctionsConfiguration(target, "container1");
        Assert.Collection(target.Keys.OrderBy(k => k),
            k => Assert.Equal("Aspire__Microsoft__Azure__Cosmos__container1__AccountEndpoint", k),
            k => Assert.Equal("container1__accountEndpoint", k));
    }
}
