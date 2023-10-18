// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Storage.Blobs.Tests;

public class ConformanceTests : ConformanceTests<BlobServiceClient, AzureStorageBlobsSettings>
{
    // Authentication method: Azure AD User Account
    // Roles: Storage Blob Data Reader, Storage Blob Data Contributor
    public const string ServiceUri = "https://aspirestoragetests.blob.core.windows.net/";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Storage.Blobs.BlobContainerClient";

    protected override string[] RequiredLogCategories => new string[]
    {
        "Azure.Core",
        "Azure.Identity"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string JsonSchemaPath => "src/Components/Aspire.Azure.Storage.Blobs/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Storage": {
                "Blobs": {
                  "ServiceUri": "http://YOUR_URI",
                  "HealthChecks": false,
                  "Tracing": true,
                  "ClientOptions": {
                    "TrimBlobNameSlashes": true,
                    "Retry": {
                      "Mode": "Exponential",
                      "Delay": "PT3S"
                    }
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "Storage":{ "Blobs": { "ServiceUri": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Storage":{ "Blobs": { "ServiceUri": "http://YOUR_URI", "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "Storage":{ "Blobs": { "ServiceUri": "http://YOUR_URI", "ClientOptions": {"Retry": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "Storage":{ "Blobs": { "ServiceUri": "http://YOUR_URI", "ClientOptions": {"Retry": {"NetworkTimeout": "3S"}}}}}}}""", "Value does not match format \"duration\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:Storage:Blobs", key, "ServiceUri"), ServiceUri),
            new(CreateConfigKey("Aspire:Azure:Storage:Blobs", key, "ClientOptions:Retry:MaxRetries"), "0")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureStorageBlobsSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureBlobService("blob", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureBlobService(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureStorageBlobsSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void SetHealthCheck(AzureStorageBlobsSettings settings, bool enabled)
        => settings.HealthChecks = enabled;

    protected override void SetMetrics(AzureStorageBlobsSettings settings, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureStorageBlobsSettings settings, bool enabled)
        => settings.Tracing = enabled;

    protected override void TriggerActivity(BlobServiceClient service)
    {
        string containerName = Guid.NewGuid().ToString();
        service.CreateBlobContainer(containerName);
        service.DeleteBlobContainer(containerName);
    }

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null), EnableTracingForAzureSdk()).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key"), EnableTracingForAzureSdk()).Dispose();

    private static bool GetCanConnect()
    {
        BlobClientOptions clientOptions = new();
        clientOptions.Retry.MaxRetries = 0; // don't enable retries (test runs few times faster)
        BlobServiceClient tableClient = new(new Uri(ServiceUri), new DefaultAzureCredential(), clientOptions);

        try
        {
            tableClient.GetBlobContainers().AsPages(pageSizeHint: 1).FirstOrDefault();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
