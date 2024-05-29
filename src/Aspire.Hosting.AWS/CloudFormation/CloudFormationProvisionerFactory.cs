// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

internal class CloudFormationProvisionerFactory : ICloudFormationProvisionerFactory
{
    public CloudFormationProvisioner CreateProvisioner(CloudFormationProvisionerOptions options)
        => new(options.AppModel, options.NotificationService, options.LoggerService);
}
