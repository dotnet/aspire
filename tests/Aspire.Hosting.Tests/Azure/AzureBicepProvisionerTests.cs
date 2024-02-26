// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureBicepProvisionerTests
{
    [Fact]
    public void SetParametersTranslatesParametersToARMCompatibleJsonParameters()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "david");

        var parameters = new JsonObject();
        BicepProvisioner.SetParameters(parameters, bicep0.Resource);

        Assert.Single(parameters);
        Assert.Equal("david", parameters["name"]?["value"]?.ToString());
    }

    [Fact]
    public void SetParametersTranslatesCompatibleParameterTypes()
    {
        var builder = DistributedApplication.CreateBuilder();

        var connectionStringResource = builder.CreateResourceBuilder(
            new ResourceWithConnectionString("A", "connection string"));

        var param = builder.AddParameter("param", () => "paramValue");

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "john")
               .WithParameter("age", () => 20)
               .WithParameter("values", ["a", "b", "c"])
               .WithParameter("conn", connectionStringResource)
               .WithParameter("jsonObj", new JsonObject { ["key"] = "value" })
               .WithParameter("param", param);

        var parameters = new JsonObject();
        BicepProvisioner.SetParameters(parameters, bicep0.Resource);

        Assert.Equal(6, parameters.Count);
        Assert.Equal("john", parameters["name"]?["value"]?.ToString());
        Assert.Equal(20, parameters["age"]?["value"]?.GetValue<int>());
        Assert.Equal(["a", "b", "c"], parameters["values"]?["value"]?.AsArray()?.Select(v => v?.ToString()) ?? []);
        Assert.Equal("connection string", parameters["conn"]?["value"]?.ToString());
        Assert.Equal("value", parameters["jsonObj"]?["value"]?["key"]?.ToString());
        Assert.Equal("paramValue", parameters["param"]?["value"]?.ToString());
    }

    [Fact]
    public void ResourceWithTheSameBicepTemplateAndParametersHaveTheSameCheckSum()
    {
        var builder = DistributedApplication.CreateBuilder();

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
        BicepProvisioner.SetParameters(parameters0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0);

        var parameters1 = new JsonObject();
        BicepProvisioner.SetParameters(parameters1, bicep1.Resource);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1);

        Assert.Equal(checkSum0, checkSum1);
    }

    [Fact]
    public void ResourceWithSameTemplateButDifferentParametersHaveDifferentChecksums()
    {
        var builder = DistributedApplication.CreateBuilder();

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
        BicepProvisioner.SetParameters(parameters0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0);

        var parameters1 = new JsonObject();
        BicepProvisioner.SetParameters(parameters1, bicep1.Resource);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1);

        Assert.NotEqual(checkSum0, checkSum1);
    }

    [Fact]
    public void GetCurrentChecksumSkipsKnownValuesForCheckSumCreation()
    {
        var builder = DistributedApplication.CreateBuilder();

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
        BicepProvisioner.SetParameters(parameters0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0);

        // Save the old version of this resource's parameters to config
        var config = new ConfigurationManager();
        config["Parameters"] = parameters0.ToJsonString();

        var checkSum1 = BicepProvisioner.GetCurrentChecksum(bicep1.Resource, config);

        Assert.Equal(checkSum0, checkSum1);
    }

    private sealed class ResourceWithConnectionString(string name, string connectionString) :
        Resource(name),
        IResourceWithConnectionString
    {
        public string? GetConnectionString() => connectionString;
    }
}
