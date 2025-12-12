// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation for the creation of an Azure App Service website and its deployment slot, including
/// configuration customization.
/// <param name="hostName">The host name of the Azure App Service website.</param>
/// <param name="slotHostName">The host name of the Azure App Service deployment slot, if applicable.</param>
/// </summary>
internal sealed class AzureAppServiceWebsiteHostNameAnnotation (string hostName, string? slotHostName = null)
    : IResourceAnnotation
{
    internal string HostName { get; } = hostName;
    internal string? SlotHostName { get; } = slotHostName;
}
