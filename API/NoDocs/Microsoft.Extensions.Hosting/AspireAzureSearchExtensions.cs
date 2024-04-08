// Assembly 'Aspire.Azure.Search.Documents'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Search.Documents;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireAzureSearchExtensions
{
    public static void AddAzureSearchClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureSearchSettings>? configureSettings = null, Action<IAzureClientBuilder<SearchIndexClient, SearchClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureSearchClient(this IHostApplicationBuilder builder, string name, Action<AzureSearchSettings>? configureSettings = null, Action<IAzureClientBuilder<SearchIndexClient, SearchClientOptions>>? configureClientBuilder = null);
}
