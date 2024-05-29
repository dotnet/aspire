// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

internal interface ICloudFormationProvisionerFactory
{
    CloudFormationProvisioner CreateProvisioner(CloudFormationProvisionerOptions options);
}
