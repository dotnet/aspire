// Assembly 'Aspire.Azure.Storage.Blobs'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Storage.Blobs;

public sealed class AzureStorageBlobsSettings : IConnectionStringSettings
{
    public string? ConnectionString { get; set; }
    public Uri? ServiceUri { get; set; }
    public TokenCredential? Credential { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public AzureStorageBlobsSettings();
}
