// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Annotation that stores the storage account reference for deployment script configuration.
/// </summary>
internal sealed class AdminDeploymentScriptStorageAnnotation(AzureStorageResource storage) : IResourceAnnotation
{
    /// <summary>
    /// Gets the storage account resource used for deployment scripts.
    /// </summary>
    public AzureStorageResource Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));
}
