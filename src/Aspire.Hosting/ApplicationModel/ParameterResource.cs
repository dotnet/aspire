// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a parameter resource.
/// </summary>
public sealed class ParameterResource : Resource, IManifestExpressionProvider, IValueProvider
{
    private string? _value;
    private bool _hasValue;
    private readonly Func<ParameterResource, string> _valueGetter;

    /// <summary>
    /// Initializes a new instance of <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="name">The name of the parameter resource.</param>
    /// <param name="callback">The callback function to retrieve the value of the parameter.</param>
    /// <param name="secret">A flag indicating whether the parameter is secret.</param>
    public ParameterResource(string name, Func<ParameterResource, string> callback, bool secret = false) : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);

        _valueGetter = callback;

        ValueInput = new ParameterInput("value", secret);
    }

    /// <summary>
    /// Gets the value of the parameter.
    /// </summary>
    public string Value
    {
        get
        {
            if (!_hasValue)
            {
                _value = _valueGetter(this);
                _hasValue = true;
            }
            return _value!;
        }
    }

    /// <summary>
    /// Gets the <see cref="ParameterInput"/> that represents the value of the parameter.
    /// </summary>
    public ParameterInput ValueInput { get; }

    /// <summary>
    /// Gets a value indicating whether the parameter is secret.
    /// </summary>
    public bool Secret => ValueInput.Secret;

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is a connection string.
    /// </summary>
    public bool IsConnectionString { get; set; }

    /// <summary>
    /// Gets the expression used in the manifest to reference the value of the parameter.
    /// </summary>
    public string ValueExpression => $"{{{Name}.value}}";

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(Value);
}
