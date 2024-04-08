// Assembly 'Aspire.Microsoft.Azure.Cosmos'

using System;
using System.Runtime.CompilerServices;
using Azure.Core;

namespace Aspire.Microsoft.Azure.Cosmos;

public sealed class AzureCosmosDBSettings
{
    public string? ConnectionString { get; set; }
    public bool Tracing { get; set; }
    public Uri? AccountEndpoint { get; set; }
    public TokenCredential? Credential { get; set; }
    public AzureCosmosDBSettings();
}
