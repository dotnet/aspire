// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Extension methods for applying dashboard annotations to resources.
/// </summary>
public static class DashboardResourceExtensions
{
    /// <summary>
    /// Adds a callback to configure the dashboard context for a resource.
    /// </summary>
    /// <typeparam name="TResource">The resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="initialState">The callback to create the initial <see cref="DashboardResourceState"/>.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<TResource> WithDashboardState<TResource>(this IResourceBuilder<TResource> builder, Func<DashboardResourceState>? initialState = null)
        where TResource : IResource
    {
        initialState ??= () => DashboardResourceState.Create(builder.Resource);

        return builder.WithAnnotation(new DashboardAnnotation(initialState), ResourceAnnotationMutationBehavior.Replace);
    }
}
