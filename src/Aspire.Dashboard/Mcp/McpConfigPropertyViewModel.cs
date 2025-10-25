// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Components.Controls;

namespace Aspire.Dashboard.Mcp;

[DebuggerDisplay("Name = {Name}, Value = {Value}")]
public sealed class McpConfigPropertyViewModel : IPropertyGridItem
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}
