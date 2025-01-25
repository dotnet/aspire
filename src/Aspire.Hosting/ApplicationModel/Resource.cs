// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an abstract resource that can be used by an application, that implements <see cref="IResource"/>.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public abstract class Resource : IResource, IEquatable<Resource>, IEquatable<IResource>
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public virtual string Name { get; }

    /// <summary>
    /// Gets the annotations associated with the resource.
    /// </summary>
    public virtual ResourceAnnotationCollection Annotations { get; }

    /// <summary>
    /// Gets the type of the resource.
    /// </summary>
    public virtual Type ResourceKind => Annotations.OfType<ResourceTypeAnnotation>().LastOrDefault()?.ResourceKind ?? GetType();

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    protected Resource(string name) : this(name, [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="resourceAnnotations">The annotations associated with the resource.</param>
    protected Resource(string name, ResourceAnnotationCollection resourceAnnotations)
    {
        ModelName.ValidateName(nameof(Resource), name);

        Name = name;
        Annotations = resourceAnnotations;
        Annotations.Add(new ResourceTypeAnnotation(GetType()));
    }

    // We want equality to be based on the ResourceAnnotationCollection instance, not the default object equality.

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is IResource other && Annotations == other.Annotations;

    /// <inheritdoc />
    public override int GetHashCode() => Annotations.GetHashCode();

    // Handle == and != operators to ensure that they behave as expected.

    /// <inheritdoc />
    public static bool operator ==(Resource? left, Resource? right) => Equals(left, right);

    /// <inheritdoc />
    public static bool operator !=(Resource? left, Resource? right) => !Equals(left, right);

    /// <inheritdoc />
    public static bool operator ==(Resource? left, IResource? right) => Equals(left, right);

    /// <inheritdoc />
    public static bool operator !=(Resource? left, IResource? right) => !Equals(left, right);

    private string DebuggerToString()
    {
        return $@"Type = {GetType().Name}, Name = ""{Name}"", Annotations = {Annotations.Count}";
    }

    bool IEquatable<Resource>.Equals(Resource? other)
    {
        return other is not null && Annotations == other.Annotations;
    }

    bool IEquatable<IResource>.Equals(IResource? other)
    {
        return other is Resource resource && Equals(resource);
    }
}
