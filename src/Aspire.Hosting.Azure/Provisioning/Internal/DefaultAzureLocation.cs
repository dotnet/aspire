// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager.Resources.Models;


namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IAzureLocation"/>.
/// </summary>
internal sealed class DefaultAzureLocation(AzureLocation azureLocation) : IAzureLocation
{
    public string Name => azureLocation.Name;

    public override string ToString() => azureLocation.ToString();
}