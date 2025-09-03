// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using YamlDotNet.Core;

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
    /// Sets the command for the executable resource.
    /// </summary>
    /// <typeparam name="T">Type of executable resource.</typeparam>
    /// <param name="builder">Builder for the executable resource.</param>
    /// <param name="command">Command.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithCommand<T>(this IResourceBuilder<T> builder, string command) where T : ExecutableResource
    {
        ArgumentNullException.ThrowIfNull(command);

        var executableAnnotation = builder.Resource.Annotations.OfType<ExecutableAnnotation>().LastOrDefault();
        if (executableAnnotation is { })
        {
            executableAnnotation.Command = command;
        }
        else
        {
            executableAnnotation = new ExecutableAnnotation
            {
                Command = command,
                WorkingDirectory = string.Empty
            };
            builder.Resource.Annotations.Add(executableAnnotation);            
        }

        return builder;
    }

    /// <summary>
    /// Sets the working directory for the executable resource.
    /// </summary>
    /// <typeparam name="T">Type of executable resource.</typeparam>
    /// <param name="builder">Builder for the executable resource.</param>
    /// <param name="workingDirectory">Working directory.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithWorkingDirectory<T>(this IResourceBuilder<T> builder, string workingDirectory) where T : ExecutableResource
    {
        ArgumentNullException.ThrowIfNull(workingDirectory);

        if (builder.Resource.Annotations.OfType<ExecutableAnnotation>().LastOrDefault() is { } executableAnnotation)
        {
            executableAnnotation.WorkingDirectory = workingDirectory;
            return builder;
        }

        throw new InvalidOperationException($"The resource '{builder.Resource.Name}' is missing the ExecutableAnnotation");
    }
}
