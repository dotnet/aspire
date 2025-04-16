// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents Azure Container Registry information for deployment targets.
/// </summary>
[Experimental("ASPIRECOMPUTE001")]
public interface IAzureContainerRegistry : IContainerRegistry
{
    /// <summary>
    /// Gets the managed identity ID associated with the container registry.
    /// </summary>
    ReferenceExpression ManagedIdentityId { get; }
}
