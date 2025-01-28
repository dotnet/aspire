// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a parameter resource.
/// </summary>
public sealed class ParameterAnnotation : IResourceAnnotation
{
    private string? _value;

    /// <summary>
    /// Value of the parameter.
    /// </summary>
    public string Value => _value ??= ValueGetter(Default) ?? throw new InvalidOperationException("The value of the parameter has not been set.");

    /// <summary>
    /// Gets the value of the parameter.
    /// </summary>
    public required Func<ParameterDefault?, string> ValueGetter { get; set; }

    /// <summary>
    /// Represents how the default value of the parameter should be retrieved.
    /// </summary>
    public ParameterDefault? Default { get; set; }

    /// <summary>
    /// Gets a value indicating whether the parameter is secret.
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is a connection string.
    /// </summary>
    public bool IsConnectionString { get; set; }
}
