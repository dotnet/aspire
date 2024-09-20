// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Meilisearch.Tests;

public class ConformanceTests : ConformanceTests<MeilisearchClient, MeilisearchClientSettings>, IClassFixture<MeilisearchContainerFixture>
{
    private readonly MeilisearchContainerFixture _containerFixture;

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => string.Empty;

    protected override string[] RequiredLogCategories => [];

    protected override bool CanConnectToServer => RequiresDockerAttribute.IsSupported;

    protected override bool SupportsKeyedRegistrations => true;

    public ConformanceTests(MeilisearchContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    {
        var connectionString = RequiresDockerAttribute.IsSupported ?
          $"{_containerFixture.GetConnectionString()}" :
          "http://localhost:27017";

        configuration.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Meilisearch:Client", key, "Endpoint"), GetConnectionStringKeyValue(connectionString,"Endpoint")),
                new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Meilisearch:Client", key, "MasterKey"), GetConnectionStringKeyValue(connectionString,"MasterKey")),
                new KeyValuePair<string, string?>($"ConnectionStrings:{key}", $"Endpoint={connectionString}")
            ]);
    }

    internal static string GetConnectionStringKeyValue(string connectionString, string configKey)
    {
        // from the connection string, extract the key value of the configKey
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2 && keyValue[0].Equals(configKey, StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1];
            }
        }
        return string.Empty;
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MeilisearchClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddMeilisearchClient("meilisearch", configureSettings: configure);
        }
        else
        {
            builder.AddKeyedMeilisearchClient(key, configureSettings: configure);
        }
    }

    protected override string ValidJsonConfig => """
                                                 {
                                                   "Aspire": {
                                                     "Meilisearch": {
                                                       "Client": {
                                                         "Endpoint": "http://localhost:19530",
                                                         "MasterKey": "p@ssw0rd1"
                                                       }
                                                     }
                                                   }
                                                 }
                                                 """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Meilisearch":{ "Client": { "Endpoint": 3 }}}}""", "Value is \"integer\" but should be \"string\""),
            ("""{"Aspire": { "Meilisearch":{ "Client": { "Endpoint": "hello" }}}}""", "Value does not match format \"uri\"")
        };

    protected override void SetHealthCheck(MeilisearchClientSettings options, bool enabled)
    {
        options.DisableHealthChecks = !enabled;
    }

    protected override void SetMetrics(MeilisearchClientSettings options, bool enabled)
    {
        throw new NotImplementedException();
    }

    protected override void SetTracing(MeilisearchClientSettings options, bool enabled)
    {
        throw new NotImplementedException();
    }

    protected override void TriggerActivity(MeilisearchClient service)
    {
        using var source = new CancellationTokenSource(100);

        service.GetVersionAsync(source.Token).Wait();
    }
}
