// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Represents the correlation filter expression.
/// </summary>
public class ServiceBusCorrelationFilter
{
    private readonly OptionalValue<Dictionary<string, object>> _applicationProperties = new();
    private readonly OptionalValue<string> _correlationId = new();
    private readonly OptionalValue<string> _messageId = new();
    private readonly OptionalValue<string> _sendTo = new();
    private readonly OptionalValue<string> _replyTo = new();
    private readonly OptionalValue<string> _subject = new();
    private readonly OptionalValue<string> _sessionId = new();
    private readonly OptionalValue<string> _replyToSessionId = new();
    private readonly OptionalValue<string> _contentType = new();
    private readonly OptionalValue<bool> _requiresPreprocessing = new();

    /// <summary>
    /// Represents the correlation filter expression.
    /// </summary>
    public ServiceBusCorrelationFilter()
    {
    }

    /// <summary>
    /// Dictionary object for custom filters.
    /// </summary>
    public OptionalValue<Dictionary<string, object>> ApplicationProperties
    {
        get { return _applicationProperties; }
        set { _applicationProperties.Assign(value); }
    }

    /// <summary>
    /// Identifier of the correlation.
    /// </summary>
    public OptionalValue<string> CorrelationId
    {
        get { return _correlationId; }
        set { _correlationId.Assign(value); }
    }

    /// <summary>
    /// Identifier of the message.
    /// </summary>
    public OptionalValue<string> MessageId
    {
        get { return _messageId; }
        set { _messageId.Assign(value); }
    }

    /// <summary>
    /// Address to send to.
    /// </summary>
    public OptionalValue<string> SendTo
    {
        get { return _sendTo; }
        set { _sendTo.Assign(value); }
    }

    /// <summary>
    /// Address of the queue to reply to.
    /// </summary>
    public OptionalValue<string> ReplyTo
    {
        get { return _replyTo; }
        set { _replyTo.Assign(value); }
    }

    /// <summary>
    /// Application specific label.
    /// </summary>
    public OptionalValue<string> Subject
    {
        get { return _subject; }
        set { _subject.Assign(value); }
    }

    /// <summary>
    /// Session identifier.
    /// </summary>
    public OptionalValue<string> SessionId
    {
        get { return _sessionId; }
        set { _sessionId.Assign(value); }
    }

    /// <summary>
    /// Session identifier to reply to.
    /// </summary>
    public OptionalValue<string> ReplyToSessionId
    {
        get { return _replyToSessionId; }
        set { _replyToSessionId.Assign(value); }
    }

    /// <summary>
    /// Content type of the message.
    /// </summary>
    public OptionalValue<string> ContentType
    {
        get { return _contentType; }
        set { _contentType.Assign(value); }
    }

    /// <summary>
    /// Value that indicates whether the rule action requires preprocessing.
    /// </summary>
    public OptionalValue<bool> RequiresPreprocessing
    {
        get { return _requiresPreprocessing; }
        set { _requiresPreprocessing.Assign(value); }
    }
}
