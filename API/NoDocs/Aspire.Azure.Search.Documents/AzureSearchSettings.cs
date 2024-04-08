// Assembly 'Aspire.Azure.Search.Documents'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Search.Documents;

public sealed class AzureSearchSettings : IConnectionStringSettings
{
    public Uri? Endpoint { get; set; }
    public TokenCredential? Credential { get; set; }
    public string? Key { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public AzureSearchSettings();
}
