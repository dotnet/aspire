// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax.Inlines;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public class ResourceInline : LeafInline
{
    public required string ResourceName { get; init; }
    public required ResourceViewModel Resource { get; init; }
}
