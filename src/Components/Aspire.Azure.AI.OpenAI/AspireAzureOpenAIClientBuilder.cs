// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// A builder for configuring an <see cref="AzureOpenAIClient"/> service registration.
/// </summary>
public class AspireAzureOpenAIClientBuilder
{
    /// <summary>
    /// Constructs a new instance of <see cref="AspireAzureOpenAIClientBuilder"/>.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostApplicationBuilder"/> with which services are being registered.</param>
    /// <param name="connectionName">The name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="serviceKey">The service key used to register the <see cref="AzureOpenAIClient"/> service, if any.</param>
    public AspireAzureOpenAIClientBuilder(IHostApplicationBuilder hostBuilder, string connectionName, string? serviceKey)
    {
        HostBuilder = hostBuilder;
        ConnectionName = connectionName;
        ServiceKey = serviceKey;
    }

    /// <summary>
    /// Gets the <see cref="IHostApplicationBuilder"/> with which services are being registered.
    /// </summary>
    public IHostApplicationBuilder HostBuilder { get; }

    /// <summary>
    /// Gets the name used to retrieve the connection string from the ConnectionStrings configuration section.
    /// </summary>
    public string ConnectionName { get; }

    /// <summary>
    /// Gets the service key used to register the <see cref="AzureOpenAIClient"/> service, if any.
    /// </summary>
    public string? ServiceKey { get; }
}