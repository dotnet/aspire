// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Used to annotate resources as being potentially deployable by the Azure Provisioner.
/// </summary>
/// <param name="resource"></param>
public class AzureBicepResourceAnnotation(AzureBicepResource resource) : IResourceAnnotation
{
    /// <summary>
    /// The <see cref="AzureBicepResource"/> derived resource.
    /// </summary>
    public AzureBicepResource Resource => resource;
}
