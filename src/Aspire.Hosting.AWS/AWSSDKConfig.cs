// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;

namespace Aspire.Hosting.AWS;

internal sealed class AWSSDKConfig : IAWSSDKConfig
{
    /// <inheritdoc/>
    public string? Profile { get; set; }

    /// <inheritdoc/>
    public RegionEndpoint? Region { get; set; }
}
