// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Model.Assistant.Ghcp;

[DebuggerDisplay("Name = {Name}, Family = {Family}, DisplayName = {DisplayName}")]
public class GhcpModelResponse
{
    public string? Name { get; set; }
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
    public string? Family { get; set; }
}
