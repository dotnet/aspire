// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ComponentMetadata
{
    public required Type Type { get; init; }
    public Dictionary<string, object> Parameters { get; } = [];
}
