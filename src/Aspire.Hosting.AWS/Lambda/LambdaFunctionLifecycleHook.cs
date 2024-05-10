// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.AWS.Lambda;

internal sealed class LambdaFunctionLifecycleHook(
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService notificationService) : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        if (executionContext.IsPublishMode)
        {
            return;
        }

        foreach (var function in appModel.Resources.OfType<LambdaFunction>())
        {
            var annotation = function.Annotations.OfType<LambdaRuntimeEnvironmentAnnotation>().SingleOrDefault();

            var pathAndQuery = "";

            if (!string.IsNullOrEmpty(annotation?.PathAndQuery))
            {
                pathAndQuery = "/" + Uri.EscapeDataString(annotation.PathAndQuery);
            }

            await notificationService
                .PublishUpdateAsync(function, state => state with
                {
                    State = annotation != null ? Constants.ResourceStateRunning : Constants.ResourceStateHidden,
                    Urls = annotation?.EndpointReferences.Select(x => new UrlSnapshot(x.EndpointName, $"{x.Url}{pathAndQuery}", IsInternal: false)).ToImmutableArray() ?? []
                })
                .ConfigureAwait(false);
        }
    }
}
