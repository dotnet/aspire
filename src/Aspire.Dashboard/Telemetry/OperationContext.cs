// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public sealed class OperationContext
{
    public static readonly OperationContext Empty = new OperationContext();

    private OperationContext()
    {
    }

    public static OperationContext Create(int propertyCount)
    {
        var properties = new OperationContextProperty[propertyCount];
        for (var i = 0; i < propertyCount; i++)
        {
            properties[i] = new OperationContextProperty();
        }

        var context = new OperationContext
        {
            Properties = properties
        };

        return context;
    }

    public OperationContextProperty[] Properties { get; init; } = [];
}
