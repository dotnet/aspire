// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A URL that should be displayed for a resource.
/// </summary>
/// <param name="Url">The URL.</param>
/// <param name="Name">The name of the URL. When rendered as a link this will be used as the linked text.</param>
/// <param name="DisplayOrder">The display order the URL. Higher values mean sort higher in the list.</param>
public sealed record ResourceUrl(string Url, string Name, int? DisplayOrder)
{

}
