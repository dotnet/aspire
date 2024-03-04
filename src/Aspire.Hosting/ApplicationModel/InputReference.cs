// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an input reference for a resource with inputs.
/// </summary>
/// <param name="owner">The resource with inputs that owns the input.</param>
/// <param name="input">The <see cref="InputAnnotation"/>.</param>
public sealed class InputReference(IResource owner, InputAnnotation input) : IManifestExpressionProvider
{
    /// <summary>
    /// Gets the owner of the input.
    /// </summary>
    public IResource Owner { get; } = owner;

    /// <summary>
    /// The instance of the input resource.
    /// </summary>
    public InputAnnotation Input { get; } = input;

    /// <summary>
    /// Gets the name of the input associated with the input reference.
    /// </summary>
    public string InputName => Input.Name;

    /// <summary>
    /// Gets the expression used in the manifest to reference the value of the input.
    /// </summary>
    public string ValueExpression => $"{{{Owner.Name}.inputs.{InputName}}}";
}
