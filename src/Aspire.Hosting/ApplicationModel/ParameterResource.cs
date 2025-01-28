// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a parameter resource.
/// </summary>
public class ParameterResource : Resource, IManifestExpressionProvider, IValueProvider
{
    private ParameterAnnotation Annotation => Annotations.OfType<ParameterAnnotation>().Last();

    /// <summary>
    /// Initializes a new instance of <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="name">The name of the parameter resource.</param>
    /// <param name="callback">The callback function to retrieve the value of the parameter.</param>
    /// <param name="secret">A flag indicating whether the parameter is secret.</param>
    public ParameterResource(string name, Func<ParameterDefault?, string> callback, bool secret = false) : base(name)
    {
        Annotations.Add(new ParameterAnnotation()
        {
            ValueGetter = callback,
            Secret = secret
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="resourceAnnotations"></param>
    public ParameterResource(string name, ResourceAnnotationCollection resourceAnnotations) : base(name, resourceAnnotations)
    {
        if (!Annotations.OfType<ParameterAnnotation>().Any())
        {
            Annotations.Add(new ParameterAnnotation()
            {
                ValueGetter = _ => throw new InvalidOperationException("The value of the parameter has not been set."),
            });
        }
    }

    /// <summary>
    /// Gets the value of the parameter.
    /// </summary>
    public string Value => Annotation.Value;

    /// <summary>
    /// Represents how the default value of the parameter should be retrieved.
    /// </summary>
    public ParameterDefault? Default
    {
        get => Annotation.Default;
        set => Annotation.Default = value;
    }

    /// <summary>
    /// Gets a value indicating whether the parameter is secret.
    /// </summary>
    public bool Secret
    {
        get => Annotation.Secret;
        set => Annotation.Secret = value;
    }

    /// <summary>
    /// Gets a value indicating whether the parameter is a connection string.
    /// </summary>
    public bool IsConnectionString
    {
        get => Annotation.IsConnectionString;
        set => Annotation.IsConnectionString = value;
    }

    /// <summary>
    /// Gets the expression used in the manifest to reference the value of the parameter.
    /// </summary>
    public string ValueExpression => $"{{{Name}.value}}";

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(Value);
}
