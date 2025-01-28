// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified container.
/// </summary>
public class ContainerResource : Resource, IResourceWithEnvironment, IResourceWithArgs, IResourceWithEndpoints, IResourceWithWaitSupport
{
    /// <summary>
    /// The container Entrypoint.
    /// </summary>
    /// <remarks><c>null</c> means use the default Entrypoint defined by the container.</remarks>
    public string? Entrypoint
    {
        get => Annotations.OfType<ContainerEntryPointAnnotation>().LastOrDefault()?.Entrypoint;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            var annotation = Annotations.OfType<ContainerEntryPointAnnotation>().LastOrDefault();
            if (annotation is null)
            {
                Annotations.Add(new ContainerEntryPointAnnotation { Entrypoint = value });
            }
            else
            {
                annotation.Entrypoint = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContainerResource"/>.
    /// </summary>
    /// <param name="name"></param>
    public ContainerResource(string name) : base(name)
    {
        // This ctor is for backward compatibility. The caller is expected to set the required annotations.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContainerResource"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="entrypoint"></param>
    public ContainerResource(string name, string entrypoint) : base(name)
    {
        Annotations.Add(new ContainerEntryPointAnnotation { Entrypoint = entrypoint });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="image"></param>
    /// <param name="tag"></param>
    /// <param name="entrypoint"></param>
    public ContainerResource(string name, string image, string? tag = null, string? entrypoint = null) : base(name)
    {
        if (entrypoint is not null)
        {
            Annotations.Add(new ContainerEntryPointAnnotation { Entrypoint = entrypoint });
        }

        Annotations.Add(new ContainerImageAnnotation { Image = image, Tag = tag });
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContainerResource"/>.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="resourceAnnotations">The resource annotations.</param>
    public ContainerResource(string name, ResourceAnnotationCollection resourceAnnotations) : base(name, resourceAnnotations)
    {
    }
}
