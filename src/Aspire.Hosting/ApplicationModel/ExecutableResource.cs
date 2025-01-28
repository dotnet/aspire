// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified executable process.
/// </summary>
public class ExecutableResource : Resource, IResourceWithEnvironment, IResourceWithArgs, IResourceWithEndpoints, IResourceWithWaitSupport
{
    private ExecutableAnnotation Annotation => Annotations.OfType<ExecutableAnnotation>().Last();
    /// <summary>
    /// Gets the command associated with this executable resource.
    /// </summary>
    public string Command => Annotation.Command;

    /// <summary>
    /// Gets the working directory for the executable resource.
    /// </summary>
    public string WorkingDirectory => Annotation.WorkingDirectory;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public ExecutableResource(string name) : base(name)
    {
    }

    /// <param name="name">The name of the resource.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">The working directory of the executable.</param>
    public ExecutableResource(string name, string command, string workingDirectory) : base(name)
    {
        Annotations.Add(new ExecutableAnnotation(command, workingDirectory));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="resourceAnnotations">The annotations associated with the resource.</param>
    public ExecutableResource(string name, ResourceAnnotationCollection resourceAnnotations)
    : base(name, resourceAnnotations)
    {
    }
}
