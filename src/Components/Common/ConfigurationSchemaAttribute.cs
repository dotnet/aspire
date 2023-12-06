// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class ConfigurationSchemaAttribute : Attribute
{
    public Type[]? Types { get; set; }
    public string[]? ConfigurationPaths { get; set; }
    public string[]? LogCategories { get; set; }
}
