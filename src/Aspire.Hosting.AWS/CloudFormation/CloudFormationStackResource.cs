// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc cref="Aspire.Hosting.AWS.CloudFormation.ICloudFormationStackResource" />
internal sealed class CloudFormationStackResource(string name, string stackName)
    : CloudFormationResource(name, stackName), ICloudFormationStackResource;
