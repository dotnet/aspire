// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a parameter resource.
/// </summary>
public class ParameterResource : Resource, IResourceWithoutLifetime, IManifestExpressionProvider, IValueProvider
{
    private string? _value;
    private bool _hasValue;
    private readonly Func<ParameterDefault?, string> _valueGetter;
    private string? _configurationKey;

    /// <summary>
    /// Initializes a new instance of <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="name">The name of the parameter resource.</param>
    /// <param name="callback">The callback function to retrieve the value of the parameter.</param>
    /// <param name="secret">A flag indicating whether the parameter is secret.</param>
    public ParameterResource(string name, Func<ParameterDefault?, string> callback, bool secret = false) : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);

        _valueGetter = callback;
        Secret = secret;
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
                _value = _valueGetter(Default);
                _hasValue = true;
            }
            return _value!;
        }
    }

    /// <summary>
    /// Represents how the default value of the parameter should be retrieved.
    /// </summary>
    public ParameterDefault? Default { get; set; }

    /// <summary>
    /// Gets a value indicating whether the parameter is secret.
    /// </summary>
    public bool Secret { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is a connection string.
    /// </summary>
    public bool IsConnectionString { get; set; }

    /// <summary>
    /// Gets the expression used in the manifest to reference the value of the parameter.
    /// </summary>
    public string ValueExpression => $"{{{Name}.value}}";

    /// <summary>
    /// The configuration key for this parameter. The default format is "ConnectionStrings:{Name}" if the parameter is a connection string,
    /// otherwise it is "Parameters:{Name}".
    /// </summary>
    internal string ConfigurationKey
    {
        get => _configurationKey ?? (IsConnectionString ? $"ConnectionStrings:{Name}" : $"Parameters:{Name}");
        set => _configurationKey = value;
    }

    /// <summary>
    /// A task completion source that can be used to wait for the value of the parameter to be set.
    /// </summary>
    internal TaskCompletionSource<string>? WaitForValueTcs { get; set; }

    async ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        if (WaitForValueTcs is not null)
        {
            // Wait for the value to be set if the task completion source is available.
            return await WaitForValueTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Value;
    }
}
