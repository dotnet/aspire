// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class SDKResourceExtensions
{
    public static IResourceBuilder<ProjectResource> WithAWSProfile(this IResourceBuilder<ProjectResource> builder, string profile)
    {
        builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }

            context.EnvironmentVariables["AWS_PROFILE"] = profile;
        });

        return builder;
    }

    public static IResourceBuilder<ProjectResource> WithAWSRegion(this IResourceBuilder<ProjectResource> builder, RegionEndpoint region)
    {
        builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }

            context.EnvironmentVariables["AWS_REGION"] = region.SystemName;
        });

        return builder;
    }
}
