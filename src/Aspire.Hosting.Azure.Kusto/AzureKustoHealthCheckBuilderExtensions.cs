// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Azure.Kusto;

internal static class AzureKustoHealthCheckBuilderExtensions
{
    /// <summary>
    /// Adds a Kusto health check to the health check builder.
    /// </summary>
    /// <param name="builder">The health check builder.</param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="connectionStringFactory">The Kusto connection string builder.</param>
    /// <returns>The updated health check builder.</returns>
    public static IHealthChecksBuilder AddAzureKustoHealthCheck(this IHealthChecksBuilder builder, string name, Func<IServiceProvider, KustoConnectionStringBuilder> connectionStringFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectionStringFactory);

        var registration = new HealthCheckRegistration(name, sp => new AzureKustoHealthCheck(connectionStringFactory(sp)), failureStatus: default, tags: default, timeout: default);
        return builder.Add(registration);
    }
}
