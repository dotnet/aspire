// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Utils;

internal static class PublishingContextUtils
{
    public static string GetEnvironmentOutputPath(PublishingContext context, IComputeEnvironmentResource environment)
    {
        if (context.Model.Resources.OfType<IComputeEnvironmentResource>().Count() > 1)
        {
            // If there are multiple compute environments, append the environment name to the output path
            return Path.Combine(context.OutputPath, environment.Name);
        }

        // If there is only one compute environment, use the root output path
        return context.OutputPath;
    }
}
