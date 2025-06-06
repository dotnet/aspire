// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        await BicepUtilities.SetParametersAsync(parameters, bicep0.Resource);

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
        await BicepUtilities.SetParametersAsync(parameters, bicep0.Resource);

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
        await BicepUtilities.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = BicepUtilities.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await BicepUtilities.SetParametersAsync(parameters1, bicep1.Resource);
        var checkSum1 = BicepUtilities.GetChecksum(bicep1.Resource, parameters1, null);

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
        await BicepUtilities.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = BicepUtilities.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await BicepUtilities.SetParametersAsync(parameters1, bicep1.Resource);
        var checkSum1 = BicepUtilities.GetChecksum(bicep1.Resource, parameters1, null);

        Assert.NotEqual(checkSum0, checkSum1);
    }

    [Fact]
    public async Task GetCurrentChecksumSkipsKnownValuesForCheckSumCreation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david");

        // Simulate the case where a known parameter has a value
        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter(AzureBicepResource.KnownParameters.PrincipalId, "id")
                       .WithParameter(AzureBicepResource.KnownParameters.Location, "tomorrow")
                       .WithParameter(AzureBicepResource.KnownParameters.PrincipalType, "type");

        var parameters0 = new JsonObject();
        await BicepUtilities.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = BicepUtilities.GetChecksum(bicep0.Resource, parameters0, null);

        // Save the old version of this resource's parameters to config
        var config = new ConfigurationManager();
        config["Parameters"] = parameters0.ToJsonString();

        var checkSum1 = await BicepUtilities.GetCurrentChecksumAsync(bicep1.Resource, config);

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
        await BicepUtilities.SetParametersAsync(parameters0, bicep0.Resource);
        await BicepUtilities.SetScopeAsync(scope0, bicep0.Resource);
        var checkSum0 = BicepUtilities.GetChecksum(bicep0.Resource, parameters0, scope0);

        var parameters1 = new JsonObject();
        var scope1 = new JsonObject();
        await BicepUtilities.SetParametersAsync(parameters1, bicep1.Resource);
        await BicepUtilities.SetScopeAsync(scope1, bicep1.Resource);
        var checkSum1 = BicepUtilities.GetChecksum(bicep1.Resource, parameters1, scope1);

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
        await BicepUtilities.SetParametersAsync(parameters0, bicep0.Resource);
        await BicepUtilities.SetScopeAsync(scope0, bicep0.Resource);
        var checkSum0 = BicepUtilities.GetChecksum(bicep0.Resource, parameters0, scope0);

        var parameters1 = new JsonObject();
        var scope1 = new JsonObject();
        await BicepUtilities.SetParametersAsync(parameters1, bicep1.Resource);
        await BicepUtilities.SetScopeAsync(scope1, bicep1.Resource);
        var checkSum1 = BicepUtilities.GetChecksum(bicep1.Resource, parameters1, scope1);

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

    [Fact]
    public void ShouldProvision_ReturnsFalse_WhenResourceIsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        
        // Make the resource a container by adding the container annotation
        bicep.Annotations.Add(new ContainerImageAnnotation { Image = "test-image" });

        var result = BicepProvisioner.ShouldProvision(bicep);

        Assert.False(result);
    }

    [Fact]
    public void ShouldProvision_ReturnsTrue_WhenResourceIsNotContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;

        var result = BicepProvisioner.ShouldProvision(bicep);

        Assert.True(result);
    }

    [Fact]
    public void GetChecksum_ReturnsSameChecksum_ForSameInputs()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep1 = builder.AddBicepTemplateString("test1", "param name string").Resource;
        var bicep2 = builder.AddBicepTemplateString("test2", "param name string").Resource;
        
        var parameters = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = "value1" }
        };

        // Act
        var checksum1 = BicepUtilities.GetChecksum(bicep1, parameters, null);
        var checksum2 = BicepUtilities.GetChecksum(bicep2, parameters, null);

        // Assert
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void GetChecksum_ReturnsDifferentChecksum_ForDifferentParameters()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        
        var parameters1 = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = "value1" }
        };
        
        var parameters2 = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = "value2" }
        };

        // Act
        var checksum1 = BicepUtilities.GetChecksum(bicep, parameters1, null);
        var checksum2 = BicepUtilities.GetChecksum(bicep, parameters2, null);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void GetChecksum_ReturnsDifferentChecksum_ForDifferentScope()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        
        var parameters = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = "value1" }
        };
        
        var scope1 = new JsonObject { ["resourceGroup"] = "rg1" };
        var scope2 = new JsonObject { ["resourceGroup"] = "rg2" };

        // Act
        var checksum1 = BicepUtilities.GetChecksum(bicep, parameters, scope1);
        var checksum2 = BicepUtilities.GetChecksum(bicep, parameters, scope2);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Theory]
    [InlineData("value1", "value1", true)]
    [InlineData("value1", "value2", false)]
    [InlineData(null, null, true)]
    [InlineData("value1", null, false)]
    [InlineData(null, "value1", false)]
    public void GetChecksum_ConsistentBehavior_ForParameterComparisons(string? value1, string? value2, bool shouldEqual)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        
        var parameters1 = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = value1 }
        };
        
        var parameters2 = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = value2 }
        };

        // Act
        var checksum1 = BicepUtilities.GetChecksum(bicep, parameters1, null);
        var checksum2 = BicepUtilities.GetChecksum(bicep, parameters2, null);

        // Assert
        if (shouldEqual)
        {
            Assert.Equal(checksum1, checksum2);
        }
        else
        {
            Assert.NotEqual(checksum1, checksum2);
        }
    }

    [Fact]
    public async Task SetParametersAsync_SkipsKnownParametersWhenSkipDynamicValuesIsTrue()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        bicep.Parameters["normalParam"] = "normalValue";
        bicep.Parameters[AzureBicepResource.KnownParameters.PrincipalId] = "someId";
        bicep.Parameters[AzureBicepResource.KnownParameters.Location] = "someLocation";
        
        var parameters = new JsonObject();

        // Act
        await BicepUtilities.SetParametersAsync(parameters, bicep, skipDynamicValues: true);

        // Assert
        Assert.Single(parameters);
        Assert.True(parameters.ContainsKey("normalParam"));
        Assert.False(parameters.ContainsKey(AzureBicepResource.KnownParameters.PrincipalId));
        Assert.False(parameters.ContainsKey(AzureBicepResource.KnownParameters.Location));
    }

    [Fact]
    public async Task SetParametersAsync_IncludesAllParametersWhenSkipDynamicValuesIsFalse()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        bicep.Parameters["normalParam"] = "normalValue";
        bicep.Parameters[AzureBicepResource.KnownParameters.PrincipalId] = "someId";
        bicep.Parameters[AzureBicepResource.KnownParameters.Location] = "someLocation";
        
        var parameters = new JsonObject();

        // Act
        await BicepUtilities.SetParametersAsync(parameters, bicep, skipDynamicValues: false);

        // Assert
        Assert.Equal(3, parameters.Count);
        Assert.True(parameters.ContainsKey("normalParam"));
        Assert.True(parameters.ContainsKey(AzureBicepResource.KnownParameters.PrincipalId));
        Assert.True(parameters.ContainsKey(AzureBicepResource.KnownParameters.Location));
    }

    [Fact]
    public async Task SetScopeAsync_SetsResourceGroupFromScope()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        bicep.Scope = new("test-rg");
        
        var scope = new JsonObject();

        // Act
        await BicepUtilities.SetScopeAsync(scope, bicep);

        // Assert
        Assert.Single(scope);
        Assert.Equal("test-rg", scope["resourceGroup"]?.ToString());
    }

    [Fact]
    public async Task SetScopeAsync_SetsNullWhenNoScope()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        // No scope set
        
        var scope = new JsonObject();

        // Act
        await BicepUtilities.SetScopeAsync(scope, bicep);

        // Assert
        Assert.Single(scope);
        Assert.Null(scope["resourceGroup"]?.AsValue().GetValue<object>());
    }

    [Fact]
    public async Task GetCurrentChecksumAsync_ReturnsNullForMissingParameters()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        var config = new ConfigurationBuilder().Build();

        // Act
        var result = await BicepUtilities.GetCurrentChecksumAsync(bicep, config);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentChecksumAsync_ReturnsNullForInvalidJson()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Parameters"] = "invalid-json"
        });
        var config = configurationBuilder.Build();

        // Act
        var result = await BicepUtilities.GetCurrentChecksumAsync(bicep, config);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentChecksumAsync_ReturnsValidChecksumForValidParameters()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        bicep.Parameters["param1"] = "value1";
        
        var parameters = new JsonObject
        {
            ["param1"] = new JsonObject { ["value"] = "value1" }
        };
        
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Parameters"] = parameters.ToJsonString()
        });
        var config = configurationBuilder.Build();

        // Act
        var result = await BicepUtilities.GetCurrentChecksumAsync(bicep, config);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
