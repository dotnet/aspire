// Assembly 'Aspire.Azure.Data.Tables'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Data.Tables;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireTablesExtensions
{
    public static void AddAzureTableClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureDataTablesSettings>? configureSettings = null, Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureTableClient(this IHostApplicationBuilder builder, string name, Action<AzureDataTablesSettings>? configureSettings = null, Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null);
}
