// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// An endpoint represented by a <see cref="System.Uri"/>.
/// </summary>
/// <param name="uri">The <see cref="System.Uri"/>.</param>
public sealed class UriEndPoint(Uri uri) : EndPoint
{
    /// <summary>
    /// Gets the <see cref="System.Uri"/> associated with this endpoint.
    /// </summary>
    public Uri Uri => uri;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is UriEndPoint other && Uri.Equals(other.Uri);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Uri.GetHashCode();

    /// <inheritdoc/>
    public override string? ToString() => uri.ToString();
}
