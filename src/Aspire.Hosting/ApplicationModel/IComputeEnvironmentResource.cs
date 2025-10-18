// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a compute environment resource.
/// </summary>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IComputeEnvironmentResource : IResource
{
    /// <summary>
    /// Gets a <see cref="ReferenceExpression"/> representing the host address or host name for the specified <see cref="EndpointReference"/>.
    /// </summary>
    /// <param name="endpointReference">The endpoint reference for which to retrieve the host address or host name.</param>
    /// <returns>A <see cref="ReferenceExpression"/> representing the host address or host name (not a full URL).</returns>
    /// <remarks>
    /// The returned value typically contains only the host name or address, without scheme, port, or path information.
    /// </remarks>
    ReferenceExpression GetHostAddressExpression(EndpointReference endpointReference) => throw new NotImplementedException();
}
