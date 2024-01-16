// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Provides extension methods for adding the Aspire dashboard to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DashboardExtensions
{
    public static IResourceBuilder<ExecutableResource> AddDashboard(this IDistributedApplicationBuilder builder)
    {
        // TODO once DAM supports dotnet tool resources, this method will configure it with something like:
        //
        //    return builder.AddDotnetTool("aspire.dashboard");

        throw new NotImplementedException();
    }
}
