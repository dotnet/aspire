// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure.ServiceBus;

/// <summary>
/// Represents a Service Bus Rule.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class ServiceBusRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusRule"/> class.
    /// </summary>
    public ServiceBusRule(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The rule name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Properties of correlation filter.
    /// </summary>
    public ServiceBusCorrelationFilter CorrelationFilter { get; set; } = new();

    /// <summary>
    /// Filter type that is evaluated against a BrokeredMessage.
    /// </summary>
    public ServiceBusFilterType FilterType { get; set; } = ServiceBusFilterType.CorrelationFilter;

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.ServiceBus.ServiceBusRule"/> instance.</returns>
    internal global::Azure.Provisioning.ServiceBus.ServiceBusRule ToProvisioningEntity()
    {
        var rule = new global::Azure.Provisioning.ServiceBus.ServiceBusRule(Infrastructure.NormalizeBicepIdentifier(Name));

        if (Name != null)
        {
            rule.Name = Name;
        }

        if (CorrelationFilter != null)
        {
            rule.CorrelationFilter = new();

            foreach (var property in CorrelationFilter.Properties)
            {
                rule.CorrelationFilter.ApplicationProperties[property.Key] = property.Value;
            }
            if (CorrelationFilter.CorrelationId != null)
            {
                rule.CorrelationFilter.CorrelationId = CorrelationFilter.CorrelationId;
            }
            if (CorrelationFilter.MessageId != null)
            {
                rule.CorrelationFilter.MessageId = CorrelationFilter.MessageId;
            }
            if (CorrelationFilter.SendTo != null)
            {
                rule.CorrelationFilter.SendTo = CorrelationFilter.SendTo;
            }
            if (CorrelationFilter.ReplyTo != null)
            {
                rule.CorrelationFilter.ReplyTo = CorrelationFilter.ReplyTo;
            }
            if (CorrelationFilter.Subject != null)
            {
                rule.CorrelationFilter.Subject = CorrelationFilter.Subject;
            }
            if (CorrelationFilter.SessionId != null)
            {
                rule.CorrelationFilter.SessionId = CorrelationFilter.SessionId;
            }
            if (CorrelationFilter.ReplyToSessionId != null)
            {
                rule.CorrelationFilter.ReplyToSessionId = CorrelationFilter.ReplyToSessionId;
            }
            if (CorrelationFilter.ContentType != null)
            {
                rule.CorrelationFilter.ContentType = CorrelationFilter.ContentType;
            }
            if (CorrelationFilter.RequiresPreprocessing.HasValue)
            {
                rule.CorrelationFilter.RequiresPreprocessing = CorrelationFilter.RequiresPreprocessing.Value;
            }
        }

        rule.FilterType = FilterType switch
        {
            ServiceBusFilterType.SqlFilter => global::Azure.Provisioning.ServiceBus.ServiceBusFilterType.SqlFilter,
            ServiceBusFilterType.CorrelationFilter => global::Azure.Provisioning.ServiceBus.ServiceBusFilterType.CorrelationFilter,
            _ => throw new NotImplementedException()
        };

        return rule;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    internal void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var rule = this;

        if (rule.Name != null)
        {
            writer.WriteString(nameof(ServiceBusQueue.Name), rule.Name);
        }

        writer.WriteStartObject("Properties");

        writer.WriteString(nameof(FilterType), rule.FilterType switch
        {
            // The Emulator uses "Sql/Correlation" instead of "SqlFilter/CorrelationFilter" in Azure.Provisioning (and Bicep template).
            ServiceBusFilterType.SqlFilter => "Sql",
            ServiceBusFilterType.CorrelationFilter => "Correlation",
            _ => throw new NotImplementedException()
        });

        writer.WriteStartObject(nameof(CorrelationFilter));

        if (rule.CorrelationFilter.Properties.Count != 0)
        {
            writer.WritePropertyName("Properties");

            JsonSerializer.Serialize(writer, rule.CorrelationFilter.Properties);
        }
        if (rule.CorrelationFilter.CorrelationId != null)
        {
            writer.WriteString(nameof(ServiceBusCorrelationFilter.CorrelationId), rule.CorrelationFilter.CorrelationId);
        }
        if (rule.CorrelationFilter.MessageId != null)
        {
            writer.WriteString(nameof(ServiceBusCorrelationFilter.MessageId), rule.CorrelationFilter.MessageId);
        }
        if (rule.CorrelationFilter.SendTo != null)
        {
            // Azure.Provisioning uses "SentTo" instead of "To" accepted in the Emulator (and Bicep template).
            writer.WriteString("To", rule.CorrelationFilter.SendTo);
        }
        if (rule.CorrelationFilter.ReplyTo != null)
        {
            writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyTo), rule.CorrelationFilter.ReplyTo);
        }
        if (rule.CorrelationFilter.Subject != null)
        {
            // Azure.Provisioning uses "Subject" instead of "Label" accepted in Emulator (and Bicep template).
            writer.WriteString("Label", rule.CorrelationFilter.Subject);
        }
        if (rule.CorrelationFilter.SessionId != null)
        {
            writer.WriteString(nameof(ServiceBusCorrelationFilter.SessionId), rule.CorrelationFilter.SessionId);
        }
        if (rule.CorrelationFilter.ReplyToSessionId != null)
        {
            writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyToSessionId), rule.CorrelationFilter.ReplyToSessionId);
        }
        if (rule.CorrelationFilter.ContentType != null)
        {
            writer.WriteString(nameof(ServiceBusCorrelationFilter.ContentType), rule.CorrelationFilter.ContentType);
        }
        if (rule.CorrelationFilter.RequiresPreprocessing.HasValue)
        {
            writer.WriteBoolean(nameof(ServiceBusCorrelationFilter.RequiresPreprocessing), rule.CorrelationFilter.RequiresPreprocessing.Value);
        }

        writer.WriteEndObject(); // CorrelationFilter

        writer.WriteEndObject(); // Properties
    }
}
