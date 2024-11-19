// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Represents the filter actions which are allowed for the transformation of a
/// message that have been matched by a filter expression.
/// </summary>
public class ServiceBusFilterAction
{
    private readonly OptionalValue<string> _sqlExpression = new();
    private readonly OptionalValue<int> _compatibilityLevel = new();
    private readonly OptionalValue<bool> _requiresPreprocessing = new();

    /// <summary>
    /// Creates a new ServiceBusFilterAction.
    /// </summary>
    public ServiceBusFilterAction()
    {
    }

    /// <summary>
    /// SQL expression. e.g. MyProperty=&apos;ABC&apos;.
    /// </summary>
    public OptionalValue<string> SqlExpression
    {
        get { return _sqlExpression; }
        set { _sqlExpression.Assign(value); }
    }

    /// <summary>
    /// This property is reserved for future use. An integer value showing the
    /// compatibility level, currently hard-coded to 20.
    /// </summary>
    public OptionalValue<int> CompatibilityLevel
    {
        get { return _compatibilityLevel; }
        set { _compatibilityLevel.Assign(value); }
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
