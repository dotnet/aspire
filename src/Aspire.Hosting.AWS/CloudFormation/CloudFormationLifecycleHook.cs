// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// The lifecycle hook that handles deploying the CloudFormation template to a CloudFormation stack.
/// </summary>
/// <param name="publishingOptions"></param>
/// <param name="notificationService"></param>
/// <param name="loggerService"></param>
internal sealed class CloudFormationLifecycleHook(
    IOptions<PublishingOptions> publishingOptions,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (publishingOptions.Value.Publisher == "manifest")
        {
            return Task.CompletedTask;
        }

        _ = Task.Run(() => new CloudFormationProvisioner(appModel, notificationService, loggerService).ConfigureCloudFormation(), cancellationToken);

        return Task.CompletedTask;
    }
}

