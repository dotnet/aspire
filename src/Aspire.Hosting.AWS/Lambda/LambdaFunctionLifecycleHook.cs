// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.AWS.Lambda;

internal sealed class LambdaFunctionLifecycleHook(
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService notificationService) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        if (executionContext.IsPublishMode)
        {
            return;
        }

        foreach (var function in appModel.Resources.OfType<LambdaFunction>())
        {
            var annotation = function.Annotations.OfType<LambdaRuntimeEnvironmentAnnotation>().SingleOrDefault();

            await notificationService.PublishUpdateAsync(function,
                    state => state with { State = annotation != null ? "Running" : "Hidden" })
                .ConfigureAwait(false);
        }
    }
}
