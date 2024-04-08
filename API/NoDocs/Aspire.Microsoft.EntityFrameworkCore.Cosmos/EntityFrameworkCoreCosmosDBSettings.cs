// Assembly 'Aspire.Microsoft.EntityFrameworkCore.Cosmos'

using System;
using System.Runtime.CompilerServices;
using Azure.Core;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos;

public sealed class EntityFrameworkCoreCosmosDBSettings
{
    public string? ConnectionString { get; set; }
    public Uri? AccountEndpoint { get; set; }
    public TokenCredential? Credential { get; set; }
    public bool Tracing { get; set; }
    public string? Region { get; set; }
    public TimeSpan? RequestTimeout { get; set; }
    public EntityFrameworkCoreCosmosDBSettings();
}
