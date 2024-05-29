// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.CDK;

internal class CDKProvisionerFactory : ICloudFormationProvisionerFactory
{
    public CloudFormationProvisioner CreateProvisioner(CloudFormationProvisionerOptions options)
        => new CDKProvisioner(options.AppModel, options.NotificationService, options.LoggerService);
}
