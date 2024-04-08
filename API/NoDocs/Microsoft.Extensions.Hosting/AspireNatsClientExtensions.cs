// Assembly 'Aspire.NATS.Net'

using System;
using Aspire.NATS.Net;
using NATS.Client.Core;

namespace Microsoft.Extensions.Hosting;

public static class AspireNatsClientExtensions
{
    public static void AddNatsClient(this IHostApplicationBuilder builder, string connectionName, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null);
    public static void AddKeyedNatsClient(this IHostApplicationBuilder builder, string name, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null);
    public static void AddNatsJetStream(this IHostApplicationBuilder builder);
}
