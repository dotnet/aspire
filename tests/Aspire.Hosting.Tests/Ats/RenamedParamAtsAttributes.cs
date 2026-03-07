// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Third-party mock attributes with renamed constructor parameters.
// Verifies that signature-based matching (arity + type) works
// even when parameter names differ from the official attributes.

namespace Aspire.Hosting.Tests.Ats.RenamedParam;

[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly | AttributeTargets.Property,
    Inherited = false,
    AllowMultiple = true)]
public sealed class AspireExportAttribute : Attribute
{
    // Parameter is named "name" instead of "id"
    public AspireExportAttribute(string name)
    {
        Id = name;
    }

    public AspireExportAttribute()
    {
    }

    // Parameter is named "targetType" instead of "type"
    public AspireExportAttribute(Type targetType)
    {
        Type = targetType;
    }

    public string? Id { get; }
    public Type? Type { get; set; }
    public string? Description { get; set; }
    public string? MethodName { get; set; }
    public bool ExposeProperties { get; set; }
    public bool ExposeMethods { get; set; }
}
