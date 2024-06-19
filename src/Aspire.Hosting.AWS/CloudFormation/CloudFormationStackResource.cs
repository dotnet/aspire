// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc/>
internal sealed class CloudFormationStackResource(string name)
    : CloudFormationResource(name), ICloudFormationStackResource;
