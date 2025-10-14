// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for working with <see cref="ExecutableResource"/> objects.
/// </summary>
public static class ExecutableResourceExtensions
{
    /// <summary>
    /// Returns an enumerable collection of executable resources from the specified distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model to retrieve executable resources from.</param>
    /// <returns>An enumerable collection of executable resources.</returns>
    public static IEnumerable<ExecutableResource> GetExecutableResources(this DistributedApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.Resources.OfType<ExecutableResource>();
    }

    /// <summary>
    /// Adds a <see cref="ExecutableCertificateTrustCallbackAnnotation"/> to the resource annotations to associate a callback that is invoked when a certificate needs to
    /// configure itself for custom certificate trust.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">The callback to invoke when a resource needs to configure itself for custom certificate trust.</param>
    /// <returns>The updated resource builder.</returns>
    public static IResourceBuilder<TResource> WithExecutableCertificateTrustCallback<TResource>(this IResourceBuilder<TResource> builder, Func<ExecutableCertificateTrustCallbackAnnotationContext, Task> callback)
        where TResource : ExecutableResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.WithAnnotation(new ExecutableCertificateTrustCallbackAnnotation(callback), ResourceAnnotationMutationBehavior.Replace);
    }
}
