// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// The lifecycle hook that handles deploying the CloudFormation template to a CloudFormation stack.
/// </summary>
/// <param name="executionContext"></param>
/// <param name="notificationService"></param>
/// <param name="loggerService"></param>
internal sealed class CloudFormationLifecycleHook(
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsPublishMode)
        {
            return;
        }

        foreach (CloudFormationResource cloudFormationResource in appModel.Resources.OfType<CloudFormationResource>())
        {
            var state = new CustomResourceSnapshot
            {
                ResourceType = cloudFormationResource.GetType().Name,
                State = Constants.ResourceStateStarting,
                Properties = ImmutableArray.Create<(string, string)>()
            };

            await notificationService.PublishUpdateAsync(cloudFormationResource, (s) => state).ConfigureAwait(false);
            cloudFormationResource.ProvisioningTaskCompletionSource = new();
        }

        _ = Task.Run(() => new CloudFormationProvisioner(appModel, notificationService, loggerService).ConfigureCloudFormation(), cancellationToken);
    }
}

