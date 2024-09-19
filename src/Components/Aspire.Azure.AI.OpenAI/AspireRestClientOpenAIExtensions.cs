// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="OpenAIClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireRestClientOpenAIExtensions
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringIsAzure = "IsAzure";

    /// <summary>
    /// Registers <see cref="OpenAIClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// The concrete implementation is selected auto
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    public static void AddOpenAIRestApiClient(
        this IHostApplicationBuilder builder,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var useAzure = false;

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            useAzure = IsAzureConnectionString(connectionString);
        }

        if (useAzure)
        {
            builder.AddAzureOpenAIClient(connectionName);
        }
        else
        {
            builder.AddOpenAIClient(connectionName);
        }
    }

    /// <summary>
    /// Registers <see cref="OpenAIClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// The concrete implementation is selected auto
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    public static void AddKeyedOpenAIRestApiClient(
        this IHostApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var useAzure = false;

        if (builder.Configuration.GetConnectionString(name) is string connectionString)
        {
            useAzure = IsAzureConnectionString(connectionString);
        }

        if (useAzure)
        {
            builder.AddKeyedAzureOpenAIClient(name);
        }
        else
        {
            builder.AddKeyedOpenAIClient(name);
        }
    }

    private static bool IsAzureConnectionString(string connectionString)
    {
        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (connectionBuilder.ContainsKey(ConnectionStringEndpoint) && Uri.TryCreate(connectionBuilder[ConnectionStringEndpoint].ToString(), UriKind.Absolute, out var serviceUri))
        {
            if (connectionBuilder.ContainsKey(ConnectionStringIsAzure))
            {
                return bool.TryParse(connectionBuilder[ConnectionStringIsAzure].ToString(), out var isAzure) && isAzure;
            }

            if (serviceUri.Host.Contains(".azure.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
