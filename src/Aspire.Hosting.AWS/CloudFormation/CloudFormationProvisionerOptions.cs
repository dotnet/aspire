// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

internal class CloudFormationProvisionerOptions(
    DistributedApplicationModel appModel,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService)
{
    public DistributedApplicationModel AppModel { get; } = appModel;

    public ResourceNotificationService NotificationService { get; } = notificationService;

    public ResourceLoggerService LoggerService { get; } = loggerService;
}
