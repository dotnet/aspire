// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Represents a Service Bus Rule.
/// </summary>
public class ServiceBusRule
{
    private readonly OptionalValue<string> _name = new();
    private readonly OptionalValue<ServiceBusFilterAction> _action = new();
    private readonly OptionalValue<ServiceBusCorrelationFilter> _correlationFilter = new();
    private readonly OptionalValue<ServiceBusFilterType> _filterType = new();
    private readonly OptionalValue<ServiceBusSqlFilter> _sqlFilter = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusRule"/> class.
    /// </summary>
    public ServiceBusRule(string id)
    {
        Id = id;
    }

    /// <summary>
    /// The rule id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The rule name.
    /// </summary>
    public OptionalValue<string> Name
    {
        get { return _name; }
        set { _name.Assign(value); }
    }

    /// <summary>
    /// Represents the filter actions which are allowed for the transformation
    /// of a message that have been matched by a filter expression.
    /// </summary>
    public OptionalValue<ServiceBusFilterAction> Action
    {
        get { return _action; }
        set { _action.Assign(value); }
    }

    /// <summary>
    /// Properties of correlationFilter.
    /// </summary>
    public OptionalValue<ServiceBusCorrelationFilter> CorrelationFilter
    {
        get { return _correlationFilter; }
        set { _correlationFilter.Assign(value); }
    }

    /// <summary>
    /// Filter type that is evaluated against a BrokeredMessage.
    /// </summary>
    public OptionalValue<ServiceBusFilterType> FilterType
    {
        get { return _filterType; }
        set { _filterType.Assign(value); }
    }

    /// <summary>
    /// Properties of sqlFilter.
    /// </summary>
    public OptionalValue<ServiceBusSqlFilter> SqlFilter
    {
        get { return _sqlFilter; }
        set { _sqlFilter.Assign(value); }
    }

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.ServiceBus.ServiceBusRule"/> instance.</returns>
    public global::Azure.Provisioning.ServiceBus.ServiceBusRule ToProvisioningEntity()
    {
        var rule = new global::Azure.Provisioning.ServiceBus.ServiceBusRule(Id);

        if (Name.IsSet && Name.Value != null)
        {
            rule.Name = Name.Value;
        }

        if (Action.IsSet && Action.Value != null)
        {
            rule.Action = new();

            if (Action.Value.SqlExpression.IsSet && Action.Value.SqlExpression.Value != null)
            {
                rule.Action.SqlExpression = Action.Value.SqlExpression.Value;
            }
            if (Action.Value.CompatibilityLevel.IsSet)
            {
                rule.Action.CompatibilityLevel = Action.Value.CompatibilityLevel.Value;
            }
            if (Action.Value.RequiresPreprocessing.IsSet)
            {
                rule.Action.RequiresPreprocessing = Action.Value.RequiresPreprocessing.Value;
            }
        }

        if (CorrelationFilter.IsSet && CorrelationFilter.Value != null)
        {
            rule.CorrelationFilter = new();

            if (CorrelationFilter.Value.ApplicationProperties.IsSet && CorrelationFilter.Value.ApplicationProperties.Value != null)
            {
                foreach (var property in CorrelationFilter.Value.ApplicationProperties.Value)
                {
                    rule.CorrelationFilter.ApplicationProperties[property.Key] = property.Value;
                }
            }
            if (CorrelationFilter.Value.CorrelationId.IsSet && CorrelationFilter.Value.CorrelationId.Value != null)
            {
                rule.CorrelationFilter.CorrelationId = CorrelationFilter.Value.CorrelationId.Value;
            }
            if (CorrelationFilter.Value.MessageId.IsSet && CorrelationFilter.Value.MessageId.Value != null)
            {
                rule.CorrelationFilter.MessageId = CorrelationFilter.Value.MessageId.Value;
            }
            if (CorrelationFilter.Value.SendTo.IsSet && CorrelationFilter.Value.SendTo.Value != null)
            {
                rule.CorrelationFilter.SendTo = CorrelationFilter.Value.SendTo.Value;
            }
            if (CorrelationFilter.Value.ReplyTo.IsSet && CorrelationFilter.Value.ReplyTo.Value != null)
            {
                rule.CorrelationFilter.ReplyTo = CorrelationFilter.Value.ReplyTo.Value;
            }
            if (CorrelationFilter.Value.Subject.IsSet && CorrelationFilter.Value.Subject.Value != null)
            {
                rule.CorrelationFilter.Subject = CorrelationFilter.Value.Subject.Value;
            }
            if (CorrelationFilter.Value.SessionId.IsSet && CorrelationFilter.Value.SessionId.Value != null)
            {
                rule.CorrelationFilter.SessionId = CorrelationFilter.Value.SessionId.Value;
            }
            if (CorrelationFilter.Value.ReplyToSessionId.IsSet && CorrelationFilter.Value.ReplyToSessionId.Value != null)
            {
                rule.CorrelationFilter.ReplyToSessionId = CorrelationFilter.Value.ReplyToSessionId.Value;
            }
            if (CorrelationFilter.Value.ContentType.IsSet && CorrelationFilter.Value.ContentType.Value != null)
            {
                rule.CorrelationFilter.ContentType = CorrelationFilter.Value.ContentType.Value;
            }
            if (CorrelationFilter.Value.RequiresPreprocessing.IsSet)
            {
                rule.CorrelationFilter.RequiresPreprocessing = CorrelationFilter.Value.RequiresPreprocessing.Value;
            }
        }

        if (FilterType.IsSet)
        {
            rule.FilterType = FilterType.Value switch
            {
                ServiceBusFilterType.SqlFilter => global::Azure.Provisioning.ServiceBus.ServiceBusFilterType.SqlFilter,
                ServiceBusFilterType.CorrelationFilter => global::Azure.Provisioning.ServiceBus.ServiceBusFilterType.CorrelationFilter,
                _ => throw new NotImplementedException()
            };
        }

        if (SqlFilter.IsSet && SqlFilter.Value != null)
        {
            rule.SqlFilter = new();

            if (SqlFilter.Value.SqlExpression.IsSet && SqlFilter.Value.SqlExpression.Value != null)
            {
                rule.SqlFilter.SqlExpression = SqlFilter.Value.SqlExpression.Value;
            }
            if (SqlFilter.Value.CompatibilityLevel.IsSet)
            {
                rule.SqlFilter.CompatibilityLevel = SqlFilter.Value.CompatibilityLevel.Value;
            }
            if (SqlFilter.Value.RequiresPreprocessing.IsSet)
            {
                rule.SqlFilter.RequiresPreprocessing = SqlFilter.Value.RequiresPreprocessing.Value;
            }
        }

        return rule;
    }

    /// <summary>
    /// Converts the current instance to a JSON object.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write the JSON object to.</param>
    public void WriteJsonObjectProperties(Utf8JsonWriter writer)
    {
        var rule = this;

        if (rule.Name.IsSet)
        {
            writer.WriteString(nameof(ServiceBusQueue.Name), rule.Name.Value);
        }

        writer.WriteStartObject("Properties");

        if (rule.Action.IsSet && rule.Action.Value != null)
        {
            writer.WriteStartObject(nameof(Action));

            if (rule.Action.Value.SqlExpression.IsSet)
            {
                writer.WriteString(nameof(ServiceBusFilterAction.SqlExpression), rule.Action.Value.SqlExpression.Value);
            }
            if (rule.Action.Value.CompatibilityLevel.IsSet)
            {
                writer.WriteNumber(nameof(ServiceBusFilterAction.CompatibilityLevel), rule.Action.Value.CompatibilityLevel.Value);
            }
            if (rule.Action.Value.RequiresPreprocessing.IsSet)
            {
                writer.WriteBoolean(nameof(ServiceBusFilterAction.RequiresPreprocessing), rule.Action.Value.RequiresPreprocessing.Value);
            }
            writer.WriteEndObject();
        }

        if (rule.CorrelationFilter.IsSet && rule.CorrelationFilter.Value != null)
        {
            writer.WriteStartObject(nameof(CorrelationFilter));

            if (rule.CorrelationFilter.Value.ApplicationProperties.IsSet && rule.CorrelationFilter.Value.ApplicationProperties != null)
            {
                JsonSerializer.Serialize(writer, rule.CorrelationFilter.Value.ApplicationProperties.Value);
            }
            if (rule.CorrelationFilter.Value.CorrelationId.IsSet)
            {
                writer.WriteString(nameof(ServiceBusCorrelationFilter.CorrelationId), rule.CorrelationFilter.Value.CorrelationId.Value);
            }
            if (rule.CorrelationFilter.Value.MessageId.IsSet)
            {
                writer.WriteString(nameof(ServiceBusCorrelationFilter.MessageId), rule.CorrelationFilter.Value.MessageId.Value);
            }
            if (rule.CorrelationFilter.Value.SendTo.IsSet)
            {
                writer.WriteString("To", rule.CorrelationFilter.Value.SendTo.Value);
            }
            if (rule.CorrelationFilter.Value.ReplyTo.IsSet)
            {
                writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyTo), rule.CorrelationFilter.Value.ReplyTo.Value);
            }
            if (rule.CorrelationFilter.Value.Subject.IsSet)
            {
                writer.WriteString("Label", rule.CorrelationFilter.Value.Subject.Value);
            }
            if (rule.CorrelationFilter.Value.SessionId.IsSet)
            {
                writer.WriteString(nameof(ServiceBusCorrelationFilter.SessionId), rule.CorrelationFilter.Value.SessionId.Value);
            }
            if (rule.CorrelationFilter.Value.ReplyToSessionId.IsSet)
            {
                writer.WriteString(nameof(ServiceBusCorrelationFilter.ReplyToSessionId), rule.CorrelationFilter.Value.ReplyToSessionId.Value);
            }
            if (rule.CorrelationFilter.Value.ContentType.IsSet)
            {
                writer.WriteString(nameof(ServiceBusCorrelationFilter.ContentType), rule.CorrelationFilter.Value.ContentType.Value);
            }
            if (rule.CorrelationFilter.Value.RequiresPreprocessing.IsSet)
            {
                writer.WriteBoolean(nameof(ServiceBusCorrelationFilter.RequiresPreprocessing), rule.CorrelationFilter.Value.RequiresPreprocessing.Value);
            }

            writer.WriteEndObject();
        }

        if (rule.FilterType.IsSet)
        {
            writer.WriteString(nameof(FilterType), rule.FilterType.Value switch
            {
                ServiceBusFilterType.SqlFilter => "Sql",
                ServiceBusFilterType.CorrelationFilter => "Correlation",
                _ => throw new NotImplementedException()
            });
        }

        if (rule.SqlFilter.IsSet && rule.SqlFilter.Value != null)
        {
            writer.WriteStartObject(nameof(SqlFilter));

            if (rule.SqlFilter.Value.SqlExpression.IsSet)
            {
                writer.WriteString(nameof(ServiceBusSqlFilter.SqlExpression), rule.SqlFilter.Value.SqlExpression.Value);
            }
            if (rule.SqlFilter.Value.CompatibilityLevel.IsSet)
            {
                writer.WriteNumber(nameof(ServiceBusSqlFilter.CompatibilityLevel), rule.SqlFilter.Value.CompatibilityLevel.Value);
            }
            if (rule.SqlFilter.Value.RequiresPreprocessing.IsSet)
            {
                writer.WriteBoolean(nameof(ServiceBusSqlFilter.RequiresPreprocessing), rule.SqlFilter.Value.RequiresPreprocessing.Value);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
