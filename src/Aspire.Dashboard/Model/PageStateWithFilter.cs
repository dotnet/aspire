// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Model;

public class PageStateWithFilter
{
    [JsonIgnore] // avoid persisting filter beyond the URL
    public string Filter { get; set; } = string.Empty;
}
