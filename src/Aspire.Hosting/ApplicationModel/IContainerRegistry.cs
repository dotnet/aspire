// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents container registry information for deployment targets.
/// </summary>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostic/{0}")]
public interface IContainerRegistry
{
    /// <summary>
    /// Gets the name of the container registry.
    /// </summary>
    ReferenceExpression Name { get; }

    /// <summary>
    /// Gets the endpoint URL of the container registry.
    /// </summary>
    ReferenceExpression Endpoint { get; }
}
