// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure;

internal sealed class ContainerAppCustomizationAnnotation(Action<AzureResourceInfrastructure, ContainerApp> configure) : IResourceAnnotation
{
    public Action<AzureResourceInfrastructure, ContainerApp> Configure { get; } = configure;
}
