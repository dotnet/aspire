// Assembly 'Aspire.RabbitMQ.Client'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aspire.RabbitMQ.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Microsoft.Extensions.Hosting;

public static class AspireRabbitMQExtensions
{
    public static void AddRabbitMQClient(this IHostApplicationBuilder builder, string connectionName, Action<RabbitMQClientSettings>? configureSettings = null, Action<ConnectionFactory>? configureConnectionFactory = null);
    public static void AddKeyedRabbitMQClient(this IHostApplicationBuilder builder, string name, Action<RabbitMQClientSettings>? configureSettings = null, Action<ConnectionFactory>? configureConnectionFactory = null);
}
