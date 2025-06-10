// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Telemetry;

[DebuggerDisplay("Name = {Name}, Properties = {Properties.Length}")]
public sealed class OperationContext
{
    public static readonly OperationContext Empty = new(name: string.Empty);

    private OperationContext(string name)
    {
        Name = name;
    }

    public static OperationContext Create(int propertyCount, string name)
    {
        var properties = new OperationContextProperty[propertyCount];
        for (var i = 0; i < propertyCount; i++)
        {
            properties[i] = new OperationContextProperty();
        }

        var context = new OperationContext(name)
        {
            Properties = properties
        };

        return context;
    }

    public OperationContextProperty[] Properties { get; init; } = [];
    public string Name { get; }
}
