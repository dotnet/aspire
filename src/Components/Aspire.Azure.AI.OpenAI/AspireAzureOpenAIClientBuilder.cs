// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.OpenAI;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.AI.OpenAI;

/// <summary>
/// A builder for configuring an <see cref="AzureOpenAIClient"/> service registration.
/// Constructs a new instance of <see cref="AspireAzureOpenAIClientBuilder"/>.
/// </summary>
/// <param name="hostBuilder">The <see cref="IHostApplicationBuilder"/> with which services are being registered.</param>
/// <param name="connectionName">The name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
/// <param name="serviceKey">The service key used to register the <see cref="AzureOpenAIClient"/> service, if any.</param>
/// <param name="disableTracing">A flag to indicate whether tracing should be disabled.</param>
public class AspireAzureOpenAIClientBuilder(IHostApplicationBuilder hostBuilder, string connectionName, string? serviceKey, bool disableTracing)
    : AspireOpenAIClientBuilder(hostBuilder, connectionName, serviceKey, disableTracing)
{
    /// <inheritdoc />
    public override string ConfigurationSectionName => AspireAzureOpenAIExtensions.DefaultConfigSectionName;
}
