// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureBicepProvisionerTests
{
    [Fact]
    public async Task SetParametersTranslatesParametersToARMCompatibleJsonParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "david");

        var parameters = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(parameters);
        Assert.Equal("david", parameters["name"]?["value"]?.ToString());
    }

    [Fact]
    public async Task SetParametersTranslatesCompatibleParameterTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("foo", "image")
            .WithHttpEndpoint()
            .WithEndpoint("http", e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", 1023);
            });

        builder.Configuration["Parameters:param"] = "paramValue";

        var connectionStringResource = builder.CreateResourceBuilder(
            new ResourceWithConnectionString("A", "connection string"));

        var param = builder.AddParameter("param");

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "john")
               .WithParameter("age", () => 20)
               .WithParameter("values", ["a", "b", "c"])
               .WithParameter("conn", connectionStringResource)
               .WithParameter("jsonObj", new JsonObject { ["key"] = "value" })
               .WithParameter("param", param)
               .WithParameter("expr", ReferenceExpression.Create($"{param.Resource}/1"))
               .WithParameter("endpoint", container.GetEndpoint("http"));

        var parameters = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(8, parameters.Count);
        Assert.Equal("john", parameters["name"]?["value"]?.ToString());
        Assert.Equal(20, parameters["age"]?["value"]?.GetValue<int>());
        Assert.Equal(["a", "b", "c"], parameters["values"]?["value"]?.AsArray()?.Select(v => v?.ToString()) ?? []);
        Assert.Equal("connection string", parameters["conn"]?["value"]?.ToString());
        Assert.Equal("value", parameters["jsonObj"]?["value"]?["key"]?.ToString());
        Assert.Equal("paramValue", parameters["param"]?["value"]?.ToString());
        Assert.Equal("paramValue/1", parameters["expr"]?["value"]?.ToString());
        Assert.Equal("http://localhost:1023", parameters["endpoint"]?["value"]?.ToString());

        // We don't yet process relationships set via the callbacks
        // so we don't see the testResource2 nor exe1
        Assert.True(bicep0.Resource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        Assert.Collection(relationships.DistinctBy(r => (r.Resource, r.Type)),
            r =>
            {
                Assert.Equal("Reference", r.Type);
                Assert.Same(connectionStringResource.Resource, r.Resource);
            },
            r =>
            {
                Assert.Equal("Reference", r.Type);
                Assert.Same(param.Resource, r.Resource);
            },
            r =>
            {
                Assert.Equal("Reference", r.Type);
                Assert.Same(container.Resource, r.Resource);
            });
    }

    [Fact]
    public async Task ResourceWithTheSameBicepTemplateAndParametersHaveTheSameCheckSum()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"])
                       .WithParameter("jsonObj", new JsonObject { ["key"] = "value" });

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"])
                       .WithParameter("jsonObj", new JsonObject { ["key"] = "value" });

        var parameters0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource, cancellationToken: TestContext.Current.CancellationToken);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, null);

        Assert.Equal(checkSum0, checkSum1);
    }

    [Theory]
    [InlineData("1alpha")]
    [InlineData("-alpha")]
    [InlineData("")]
    [InlineData(" alpha")]
    [InlineData("alpha 123")]
    public void WithParameterDoesNotAllowParameterNamesWhichAreInvalidBicepIdentifiers(string bicepParameterName)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            builder.AddAzureInfrastructure("infrastructure", _ => { })
                   .WithParameter(bicepParameterName);
        });
    }

    [Theory]
    [InlineData("alpha")]
    [InlineData("a1pha")]
    [InlineData("_alpha")]
    [InlineData("__alpha")]
    [InlineData("alpha1_")]
    [InlineData("Alpha1_A")]
    public void WithParameterAllowsParameterNamesWhichAreValidBicepIdentifiers(string bicepParameterName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureInfrastructure("infrastructure", _ => { })
                .WithParameter(bicepParameterName);
    }

    [Fact]
    public async Task ResourceWithSameTemplateButDifferentParametersHaveDifferentChecksums()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"]);

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"])
                       .WithParameter("jsonObj", new JsonObject { ["key"] = "value" });

        var parameters0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource, cancellationToken: TestContext.Current.CancellationToken);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, null);

        Assert.NotEqual(checkSum0, checkSum1);
    }

    [Fact]
    public async Task GetCurrentChecksumSkipsKnownValuesForCheckSumCreation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName);

        // Simulate the case where a known parameter has a value
        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName, "blah")
                       .WithParameter(AzureBicepResource.KnownParameters.PrincipalId, "id")
                       .WithParameter(AzureBicepResource.KnownParameters.Location, "tomorrow")
                       .WithParameter(AzureBicepResource.KnownParameters.PrincipalType, "type");

        var parameters0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        // Save the old version of this resource's parameters to config
        var config = new ConfigurationManager();
        config["Parameters"] = parameters0.ToJsonString();

        var checkSum1 = await BicepProvisioner.GetCurrentChecksumAsync(bicep1.Resource, config, TestContext.Current.CancellationToken);

        Assert.Equal(checkSum0, checkSum1);
    }

    [Fact]
    public async Task ResourceWithDifferentScopeHaveDifferentChecksums()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("key", "value");
        bicep0.Resource.Scope = new("rg0");

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("key", "value");
        bicep1.Resource.Scope = new("rg1");

        var parameters0 = new JsonObject();
        var scope0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);
        await BicepProvisioner.SetScopeAsync(scope0, bicep0.Resource, TestContext.Current.CancellationToken);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, scope0);

        var parameters1 = new JsonObject();
        var scope1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource, cancellationToken: TestContext.Current.CancellationToken);
        await BicepProvisioner.SetScopeAsync(scope1, bicep1.Resource, TestContext.Current.CancellationToken);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, scope1);

        Assert.NotEqual(checkSum0, checkSum1);
    }

    [Fact]
    public async Task ResourceWithSameScopeHaveSameChecksums()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("key", "value");
        bicep0.Resource.Scope = new("rg0");

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("key", "value");
        bicep1.Resource.Scope = new("rg0");

        var parameters0 = new JsonObject();
        var scope0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource, cancellationToken: TestContext.Current.CancellationToken);
        await BicepProvisioner.SetScopeAsync(scope0, bicep0.Resource, TestContext.Current.CancellationToken);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, scope0);

        var parameters1 = new JsonObject();
        var scope1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource, cancellationToken: TestContext.Current.CancellationToken);
        await BicepProvisioner.SetScopeAsync(scope1, bicep1.Resource, TestContext.Current.CancellationToken);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, scope1);

        Assert.Equal(checkSum0, checkSum1);
    }

    [Fact]
    public async Task NestedChildResourcesShouldGetUpdated()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmosdb");
        var db = cosmos.AddCosmosDatabase("db");
        var entries = db.AddContainer("entries", "/id");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await foreach (var resourceEvent in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
        {
            if (resourceEvent.Resource == entries.Resource)
            {
                var parentProperty = resourceEvent.Snapshot.Properties.FirstOrDefault(x => x.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                Assert.Equal("db", parentProperty);
                return;
            }
        }

        Assert.Fail();
    }

    private sealed class ResourceWithConnectionString(string name, string connectionString) :
        Resource(name),
        IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
           ReferenceExpression.Create($"{connectionString}");
    }
}
