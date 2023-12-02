// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using NATS.Client.Hosting;

namespace Microsoft.Extensions.Hosting;

public static class NatsBuilderExtensions
{
    public static void AddNats(this IHostApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            builder.Services.AddNats(configureOpts: opts => opts with { Url = connectionString });
        }
        else
        {
            throw new InvalidOperationException($"NATS connection string not found: {connectionName}");
        }
    }
}
