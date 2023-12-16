// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc/>
internal sealed class CloudFormationResource(string name, string templatePath) : Resource(name), ICloudFormationResource
{
    /// <inheritdoc/>
    public string? Profile { get; set; }

    /// <inheritdoc/>
    public string? ProfileLocation { get; set; }

    /// <inheritdoc/>
    public RegionEndpoint? Region { get; set; }

    /// <inheritdoc/>
    public IAmazonCloudFormation? CloudFormationClient { get; set; }

    /// <inheritdoc/>
    public string TemplatePath { get; } = templatePath;

    /// <inheritdoc/>
    public List<Output>? Outputs { get; set; }
}
