// Assembly 'Aspire.Azure.Messaging.ServiceBus'

using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Messaging.ServiceBus;

public sealed class AzureMessagingServiceBusSettings : IConnectionStringSettings
{
    public string? ConnectionString { get; set; }
    public string? Namespace { get; set; }
    public TokenCredential? Credential { get; set; }
    public string? HealthCheckQueueName { get; set; }
    public string? HealthCheckTopicName { get; set; }
    public bool Tracing { get; set; }
    public AzureMessagingServiceBusSettings();
}
