// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public abstract class ConformanceTestsBase<TService, TOptions> : ConformanceTests<TService, TOptions>
    where TService : class
    where TOptions : class, new()
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => $"Azure.Messaging.EventHubs.{typeof(TService).Name}";

    protected override string[] RequiredLogCategories => ["Azure.Messaging.EventHubs"];

    protected override string ValidJsonConfig => $$"""
        {
          "Aspire": {
            "Azure": {
              "Messaging": {
                "EventHubs": {
                  "{{typeof(TService).Name}}": {
                      "DisableHealthChecks": false,
                      "BlobClientServiceKey": "blobs",
                      "ClientOptions": {
                        "ConnectionIdleTimeout": "00:01",
                        "EnableCrossEntityTransactions": true,
                        "RetryOptions": {
                          "Mode": "Fixed",
                          "MaxDelay": "00:03"
                        },
                        "TransportType": "AmqpWebSockets"
                     }
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage =>
        [
             ($$"""{"Aspire": { "Azure": { "Messaging" :{ "EventHubs": { "{{typeof(TService).Name}}": { "DisableHealthChecks": "true" } } } } } }""", "Value is \"string\" but should be \"boolean\""),
        ];

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    => configuration.AddInMemoryCollection(
    [
        new($"Aspire:Azure:Messaging:EventHubs:{typeof(TService).Name}:ConnectionString", AspireEventHubsExtensionsTests.EhConnectionString)
    ]);

    protected static RemoteInvokeOptions EnableTracingForAzureSdk()
     => new()
     {
         RuntimeConfigurationOptions = { { "Azure.Experimental.EnableActivitySource", true } }
     };
}
