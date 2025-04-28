// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Components.ConformanceTests;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

public class ConformanceTests : ConformanceTests<ElasticsearchClient, ElasticClientsElasticsearchSettings>, IClassFixture<ElasticsearchContainerFixture>
{
    private readonly ElasticsearchContainerFixture _containerFixture;

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Elastic.Transport";

    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override bool CanConnectToServer => RequiresDockerAttribute.IsSupported;

    protected override bool SupportsKeyedRegistrations => true;

    public ConformanceTests(ElasticsearchContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    {
        var connectionString = RequiresDockerAttribute.IsSupported ?
          $"{_containerFixture.GetConnectionString()}" :
          "http://elastic:password@localhost:27017";

        configuration.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Elastic:Clients:Elasticsearch", key, "Endpoint"), connectionString),
                new KeyValuePair<string, string?>($"ConnectionStrings:{key}", $"Endpoint={connectionString}")
            ]);
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<ElasticClientsElasticsearchSettings>? configure = null, string? key = null)
    {
        //The Testcontainers module creates a container that listens to requests over HTTPS.
        //To communicate with the Elasticsearch instance, developers must create a ElasticsearchClientSettings instance and set the ServerCertificateValidationCallback delegate to CertificateValidations.AllowAll.
        //Failing to do so will result in a communication failure as the .NET will reject the certificate coming from the container.
        if (key is null)
        {
            builder.AddElasticsearchClient("elasticsearch", configureSettings: configure, configureClientSettings : (c)=> c.ServerCertificateValidationCallback(CertificateValidations.AllowAll));
        }
        else
        {
            builder.AddKeyedElasticsearchClient(key, configureSettings: configure, configureClientSettings: (c) => c.ServerCertificateValidationCallback(CertificateValidations.AllowAll));
        }
    }

    protected override string ValidJsonConfig => """
                                                 {
                                                   "Aspire": {
                                                     "Elastic": {
                                                       "Clients": {
                                                         "Elasticsearch": {
                                                           "Endpoint": "http://localhost:6334",
                                                           "DisableHealthChecks": true,
                                                           "DisableTracing": false,
                                                           "DisableMetrics": false
                                                         }
                                                       }
                                                     }
                                                   }
                                                 }
                                                 """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
      {
            ("""{"Aspire": { "Elastic":{ "Clients": { "Elasticsearch": { "Endpoint": 0 }}}}}""", "Value is \"integer\" but should be \"string\""),
            ("""{"Aspire": { "Elastic":{ "Clients": { "Elasticsearch": { "Endpoint": "hello" }}}}}""", "Value does not match format \"uri\"")
        };

    protected override void SetHealthCheck(ElasticClientsElasticsearchSettings options, bool enabled)
    {
        options.DisableHealthChecks = !enabled;
        options.HealthCheckTimeout = 100;
    }

    protected override void SetMetrics(ElasticClientsElasticsearchSettings options, bool enabled)
    {
        throw new NotImplementedException();
    }

    protected override void SetTracing(ElasticClientsElasticsearchSettings options, bool enabled)
    {
        options.DisableTracing = !enabled;
    }

    protected override void TriggerActivity(ElasticsearchClient service)
    {
        using var source = new CancellationTokenSource(100);

        service.InfoAsync(source.Token).Wait();
    }

    [QuarantinedTest("https://github.com/dotnet/aspire/issues/5367")]
    public override Task HealthCheckReportsExpectedStatus(string? key)
    {
        return base.HealthCheckReportsExpectedStatus(key);
    }
}
