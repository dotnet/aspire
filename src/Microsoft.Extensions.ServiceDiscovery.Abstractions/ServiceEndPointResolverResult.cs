// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Represents the result of service endpoint resolution.
/// </summary>
/// <param name="endPoints">The endpoint collection.</param>
/// <param name="status">The status.</param>
public sealed class ServiceEndPointResolverResult(ServiceEndPointCollection? endPoints, ResolutionStatus status)
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    public ResolutionStatus Status { get; } = status;

    /// <summary>
    /// Gets a value indicating whether resolution completed successfully.
    /// </summary>
    [MemberNotNullWhen(true, nameof(EndPoints))]
    public bool ResolvedSuccessfully => Status.StatusCode is ResolutionStatusCode.Success;

    /// <summary>
    /// Gets the endpoints.
    /// </summary>
    public ServiceEndPointCollection? EndPoints { get; } = endPoints;
}
