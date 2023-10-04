// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Azure;

public static class AzureComponentExtensions
{
    public static IDistributedApplicationBuilder AddAzureProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AzureProvisioner>();
        return builder;
    }
}
