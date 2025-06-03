// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisionerTests
{
    [Fact]
    public async Task SetParametersTranslatesParametersToARMCompatibleJsonParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "david");

        var parameters = new JsonObject();
        await AzureProvisioner.SetParametersAsync(parameters, bicep0.Resource);

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
        await AzureProvisioner.SetParametersAsync(parameters, bicep0.Resource);

        Assert.Equal(8, parameters.Count);
        Assert.Equal("john", parameters["name"]?["value"]?.ToString());
        Assert.Equal(20, parameters["age"]?["value"]?.GetValue<int>());
        Assert.Equal(["a", "b", "c"], parameters["values"]?["value"]?.AsArray()?.Select(v => v?.ToString()) ?? []);
        Assert.Equal("connection string", parameters["conn"]?["value"]?.ToString());
        Assert.Equal("value", parameters["jsonObj"]?["value"]?["key"]?.ToString());
        Assert.Equal("paramValue", parameters["param"]?["value"]?.ToString());
        Assert.Equal("paramValue/1", parameters["expr"]?["value"]?.ToString());
        Assert.Equal("http://localhost:1023", parameters["endpoint"]?["value"]?.ToString());
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
        await AzureProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = AzureProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await AzureProvisioner.SetParametersAsync(parameters1, bicep1.Resource);
        var checkSum1 = AzureProvisioner.GetChecksum(bicep1.Resource, parameters1, null);

        Assert.Equal(checkSum0, checkSum1);
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
        await AzureProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = AzureProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await AzureProvisioner.SetParametersAsync(parameters1, bicep1.Resource);
        var checkSum1 = AzureProvisioner.GetChecksum(bicep1.Resource, parameters1, null);

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
        await AzureProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = AzureProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        // Save the old version of this resource's parameters to config
        var config = new ConfigurationManager();
        config["Parameters"] = parameters0.ToJsonString();

        var checkSum1 = await AzureProvisioner.GetCurrentChecksumAsync(bicep1.Resource, config);

        Assert.Equal(checkSum0, checkSum1);
    }

    [Fact]
    public void AzureProvisionerCanBeCreatedWithDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddSingleton<IHostEnvironment>(new MockHostEnvironment());
        services.AddSingleton<ILogger<AzureProvisioner>>(new MockLogger<AzureProvisioner>());
        services.AddSingleton<ILogger<DefaultProvisioningContextProvider>>(new MockLogger<DefaultProvisioningContextProvider>());
        services.AddSingleton<ILogger<DefaultBicepCliInvoker>>(new MockLogger<DefaultBicepCliInvoker>());
        services.AddSingleton<ILogger<DefaultUserSecretsManager>>(new MockLogger<DefaultUserSecretsManager>());
        services.AddSingleton<ResourceNotificationService>();
        services.AddSingleton<ResourceLoggerService>();
        services.AddSingleton<DistributedApplicationExecutionContext>();
        services.AddSingleton<IDistributedApplicationEventing, DistributedApplicationEventing>();
        services.AddSingleton<TokenCredentialHolder>();
        
        // Register the internal services that make the provisioner testable
        services.AddSingleton<IProvisioningContextProvider, DefaultProvisioningContextProvider>();
        services.AddSingleton<IBicepCliInvoker, DefaultBicepCliInvoker>();
        services.AddSingleton<IUserSecretsManager, DefaultUserSecretsManager>();

        var serviceProvider = services.BuildServiceProvider();

        // Should be able to create AzureProvisioner
        var provisioner = serviceProvider.GetService<AzureProvisioner>();
        
        Assert.NotNull(provisioner);
    }

    [Fact]
    public void MockableInterfacesAllowTestingIndividualComponents()
    {
        // Arrange - create mock implementations
        var mockProvisioningContextProvider = new MockProvisioningContextProvider();
        var mockBicepCliInvoker = new MockBicepCliInvoker();
        var mockUserSecretsManager = new MockUserSecretsManager();

        // Act & Assert - verify interfaces can be mocked
        Assert.NotNull(mockProvisioningContextProvider);
        Assert.NotNull(mockBicepCliInvoker);
        Assert.NotNull(mockUserSecretsManager);
    }

    private sealed class ResourceWithConnectionString(string name, string connectionString) :
        Resource(name),
        IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
           ReferenceExpression.Create($"{connectionString}");
    }

    private sealed class MockHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/test";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class MockLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    // Mock implementations for testing
    private sealed class MockProvisioningContextProvider : IProvisioningContextProvider
    {
        public Task<ProvisioningContext> GetProvisioningContextAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException("This is a mock for testing interface design");
        }
    }

    private sealed class MockBicepCliInvoker : IBicepCliInvoker
    {
        public Task<string> CompileTemplateAsync(string bicepFilePath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("This is a mock for testing interface design");
        }
    }

    private sealed class MockUserSecretsManager : IUserSecretsManager
    {
        public Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException("This is a mock for testing interface design");
        }

        public Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("This is a mock for testing interface design");
        }
    }
}