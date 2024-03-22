// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// The lifecycle hook that handles deploying the CloudFormation template to a CloudFormation stack.
/// </summary>
internal sealed class AWSCDKLifecycleHook(
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

        foreach (var stackResource in appModel.Resources.OfType<StackResource>())
        {
            await notificationService.PublishUpdateAsync(stackResource, (state) => state with { State = Constants.ResourceStateStarting }).ConfigureAwait(false);
            stackResource.ProvisioningTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        _ = Task.Run(() => new AWSCDKProvisioner(appModel, notificationService, loggerService).ProvisionCloudFormation(cancellationToken), cancellationToken);
    }
}
