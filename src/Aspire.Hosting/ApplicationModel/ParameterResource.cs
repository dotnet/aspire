// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a parameter resource.
/// </summary>
public sealed class ParameterResource : Resource, IManifestExpressionProvider, IValueProvider
{
    private readonly Func<string> _callback;

    /// <summary>
    /// Initializes a new instance of <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="name">The name of the parameter resource.</param>
    /// <param name="callback">The callback function to retrieve the value of the parameter.</param>
    /// <param name="secret">A flag indicating whether the parameter is secret.</param>
    public ParameterResource(string name, Func<string> callback, bool secret = false) : base(name)
    {
        _callback = callback;
        Secret = secret;

        Annotations.Add(new InputAnnotation("value", secret: secret));
    }

    /// <summary>
    /// Gets the value of the parameter.
    /// </summary>
    public string Value => _callback();

    /// <summary>
    /// Gets a value indicating whether the parameter is secret.
    /// </summary>
    public bool Secret { get; }

    /// <summary>
    /// Gets the expression used in the manifest to reference the value of the parameter.
    /// </summary>
    public string ValueExpression => $"{{{Name}.value}}";

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(Value);
}
