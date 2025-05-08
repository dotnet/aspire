// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.Projects;
using Azure.Core.Extensions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Azure.AI.Projects.Tests;

public class AspireAzureAIProjectExtensionsTests
{
    private const string ConnectionString = "fake-endpoint.api.azureml.ms;2375c413-6855-548c-bc16-d1326ab8ca77;rg-name;project-name";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RegistersServiceType(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("ConnectionStrings:projects", ConnectionString)
        });
        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }

        using var host = builder.Build();
        var azureClient = useKeyed ?
            host.Services.GetKeyedService<AIProjectClient>("projects") :
            host.Services.GetService<AIProjectClient>();

        Assert.NotNull(azureClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("ConnectionStrings:projects", ConnectionString)
        });

        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }

        using var host = builder.Build();
        var azureClient = useKeyed ?
            host.Services.GetKeyedService<AIProjectClient>("projects") :
            host.Services.GetService<AIProjectClient>();

        Assert.NotNull(azureClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionInfoCanBeSetInCode(bool useKeyed)
    {
        var endpoint = new Uri("https://fake-endpoint.api.azureml.ms/");
        var subId = "2375c413-6855-548c-bc16-d1326ab8ca77";
        var rgName = "rg-name";
        var projectName = "project-name";

        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects", settings =>
            {
                settings.Endpoint = endpoint;
                settings.SubscriptionId = subId;
                settings.ResourceGroupName = rgName;
                settings.ProjectName = projectName;
            });
        }
        else
        {
            builder.AddAzureAIProjectClient("projects", settings =>
            {
                settings.Endpoint = endpoint;
                settings.SubscriptionId = subId;
                settings.ResourceGroupName = rgName;
                settings.ProjectName = projectName;
            });
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<AIProjectClient>("projects") :
            host.Services.GetRequiredService<AIProjectClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("ConnectionStrings:projects", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:projects2", ConnectionString + "2")
        ]);

        builder.AddKeyedAzureAIProjectClient("projects");
        builder.AddKeyedAzureAIProjectClient("projects2");

        using var host = builder.Build();
        var azureClient1 = host.Services.GetRequiredKeyedService<AIProjectClient>("projects");
        var azureClient2 = host.Services.GetRequiredKeyedService<AIProjectClient>("projects2");

        Assert.NotSame(azureClient1, azureClient2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvokesCallback(bool useKeyed)
    {
        var clientCacheSize = 100;

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("ConnectionStrings:projects", ConnectionString),
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects", configureClientBuilder: BuildConfiguration);
        }
        else
        {
            builder.AddAzureAIProjectClient("projects", configureClientBuilder: BuildConfiguration);
        }

        void BuildConfiguration(IAzureClientBuilder<AIProjectClient, AIProjectClientOptions> builder)
        {
            builder.ConfigureOptions(options =>
            {
                options.ClientCacheSize = clientCacheSize;
            });
        }

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsMonitor<AIProjectClientOptions>>().Get(useKeyed ? "projects" : "Default");

        Assert.NotNull(options);
        Assert.Equal(clientCacheSize, options.ClientCacheSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BindsOptions(bool useKeyed)
    {
        var clientCacheSize = 100;

        var key = useKeyed ? ":projects" : "";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("ConnectionStrings:projects", ConnectionString),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{key}:ClientCacheSize", clientCacheSize.ToString())
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsMonitor<AIProjectClientOptions>>().Get(useKeyed ? "projects" : "Default");

        Assert.NotNull(options);
        Assert.Equal(clientCacheSize, options.ClientCacheSize);
    }

    [Fact]
    public void AddAIProjectClient_WithConnectionNameAndSettings_AppliesConnectionSpecificSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "projectstest";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            [$"Aspire:Azure:AI:Project:{connectionName}:DisableTracing"] = "true"
        });

        AzureAIProjectSettings? capturedSettings = null;
        builder.AddAzureAIProjectClient(connectionName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.True(capturedSettings.DisableTracing);
    }

    [Fact]
    public void AddAIProjectClient_WithConnectionSpecific_FavorsConnectionSpecificSettings()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "projectstest";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            // General settings
            [$"Aspire:Azure:AI:Project:DisableTracing"] = "false",
            // Connection-specific settings
            [$"Aspire:Azure:AI:Project:{connectionName}:DisableTracing"] = "true",
        });

        AzureAIProjectSettings? capturedSettings = null;
        builder.AddAzureAIProjectClient(connectionName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.True(capturedSettings.DisableTracing);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoConnectionStringThrows(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("ConnectionStrings:projects", null)
        ]);
        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }
        using var host = builder.Build();
        Assert.Throws<InvalidOperationException>(() => useKeyed ?
            host.Services.GetKeyedService<AIProjectClient>("projects") :
            host.Services.GetService<AIProjectClient>());
    }

    [Theory]
    [InlineData(true, "fake-endpoint", null, null, null)]
    [InlineData(false, "fake-endpoint", null, null, null)]
    [InlineData(true, null, "project-name", null, null)]
    [InlineData(false, null, "project-name", null, null)]
    [InlineData(true, null, null, "rg-name", null)]
    [InlineData(false, null, null, "rg-name", null)]
    [InlineData(true, null, null, null, "sub-id")]
    [InlineData(false, null, null, null, "sub-id")]
    public void IncompleteConnectionStringPartsWillThrow(bool useKeyed, string? endpoint, string? projectName, string? rgName, string? subscriptionId)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:Endpoint", endpoint),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:ProjectName", projectName),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:ResourceGroupName", rgName),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:SubscriptionId", subscriptionId)
        ]);
        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }
        using var host = builder.Build();
        Assert.Throws<InvalidOperationException>(() => useKeyed ?
            host.Services.GetKeyedService<AIProjectClient>("projects") :
            host.Services.GetService<AIProjectClient>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncompleteConnectionStringPartsWillBeIgnoredWhenConnectionStringProvided(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>($"ConnectionStrings:projects", ConnectionString),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:Endpoint", null),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:ProjectName", null),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:ResourceGroupName", null),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:SubscriptionId", null)
        ]);
        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }
        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetKeyedService<AIProjectClient>("projects") :
            host.Services.GetService<AIProjectClient>();
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionSettingsOverrideMissingConnectionString(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>($"ConnectionStrings:projects", null),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:Endpoint", "https://fake-endpoint.api.azureml.ms/"),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:ProjectName", "project-name"),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:ResourceGroupName", "rg-name"),
            new KeyValuePair<string, string?>($"Aspire:Azure:AI:Projects{(useKeyed ? ":projects":"")}:SubscriptionId", "sub-id")
        ]);
        if (useKeyed)
        {
            builder.AddKeyedAzureAIProjectClient("projects");
        }
        else
        {
            builder.AddAzureAIProjectClient("projects");
        }
        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetKeyedService<AIProjectClient>("projects") :
            host.Services.GetService<AIProjectClient>();
        Assert.NotNull(client);
    }
}
