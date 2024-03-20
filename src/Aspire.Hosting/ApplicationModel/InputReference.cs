// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an input reference for a resource with inputs.
/// </summary>
/// <param name="owner">The resource with inputs that owns the input.</param>
/// <param name="inputName">The name of the input.</param>
public sealed class InputReference(IResource owner, string inputName) : IManifestExpressionProvider, IValueProvider
{
    /// <summary>
    /// Gets the owner of the input.
    /// </summary>
    public IResource Owner { get; } = owner ?? throw new ArgumentNullException(nameof(owner));

    /// <summary>
    /// Gets the instance of the input annotation.
    /// </summary>
    public InputAnnotation Input => GetInputAnnotation();

    /// <summary>
    /// Gets the name of the input associated with the input reference.
    /// </summary>
    public string InputName { get; } = inputName ?? throw new ArgumentNullException(nameof(inputName));

    /// <inheritdoc/>
    public string ValueExpression => $"{{{Owner.Name}.inputs.{InputName}}}";

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(Input.Value);

    private InputAnnotation GetInputAnnotation()
    {
        var input = Owner.Annotations.OfType<InputAnnotation>().SingleOrDefault(a => a.Name == InputName) ??
            throw new InvalidOperationException($"The InputAnnotation '{InputName}' was not found for the resource '{Owner.Name}'.");

        return input;
    }
}
