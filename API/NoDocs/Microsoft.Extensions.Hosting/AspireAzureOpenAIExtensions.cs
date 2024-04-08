// Assembly 'Aspire.Azure.AI.OpenAI'

using System;
using Aspire.Azure.AI.OpenAI;
using Aspire.Azure.Common;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class AspireAzureOpenAIExtensions
{
    public static void AddAzureOpenAIClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureOpenAISettings>? configureSettings = null, Action<IAzureClientBuilder<OpenAIClient, OpenAIClientOptions>>? configureClientBuilder = null);
    public static void AddKeyedAzureOpenAIClient(this IHostApplicationBuilder builder, string name, Action<AzureOpenAISettings>? configureSettings = null, Action<IAzureClientBuilder<OpenAIClient, OpenAIClientOptions>>? configureClientBuilder = null);
}
