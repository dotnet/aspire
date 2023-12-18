// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS;

internal sealed class AWSSDKConfigResource(string name) : Resource(name), IAWSSDKConfigResource
{
    /// <inheritdoc/>
    public string? Profile { get; set; }

    /// <inheritdoc/>
    public RegionEndpoint? Region { get; set; }
}
