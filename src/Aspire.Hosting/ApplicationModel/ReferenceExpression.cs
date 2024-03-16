// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an expression that might be made up of multiple resource properties. For example,
/// a connection string might be made up of a host, port, and password from different endpoints.
/// </summary>
public class ReferenceExpression : IValueProvider, IManifestExpressionProvider
{
    private readonly string[] _manifestExpressions;

    private ReferenceExpression(string format, IValueProvider[] valueProviders, string[] manifestExpressions)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(valueProviders);
        ArgumentNullException.ThrowIfNull(manifestExpressions);

        Format = format;
        ValueProviders = valueProviders;
        _manifestExpressions = manifestExpressions;
    }

    /// <summary>
    /// The format string for this expression.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// The manifest expressions for the parameters for the format string.
    /// </summary>
    public IReadOnlyList<string> ManifestExpressions => _manifestExpressions;

    /// <summary>
    /// The list of <see cref="IValueProvider"/> that will be used to resolve parameters for the format string.
    /// </summary>
    public IReadOnlyList<IValueProvider> ValueProviders { get; }

    /// <summary>
    /// The value expression for the format string.
    /// </summary>
    public string ValueExpression =>
        string.Format(CultureInfo.InvariantCulture, Format, _manifestExpressions);

    /// <summary>
    /// Gets the value of the expression. The final string value after evaluating the format string and its parameters.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken)
    {
        if (Format.Length == 0)
        {
            return null;
        }

        var args = new object?[ValueProviders.Count];
        for (var i = 0; i < ValueProviders.Count; i++)
        {
            args[i] = await ValueProviders[i].GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        return string.Format(CultureInfo.InvariantCulture, Format, args);
    }

    internal static ReferenceExpression Create(string format, IValueProvider[] valueProviders, string[] manifestExpressions)
    {
        return new(format, valueProviders, manifestExpressions);
    }

    /// <summary>
    /// Creates a new instance of <see cref="ReferenceExpression"/> with the specified format and value providers.
    /// </summary>
    /// <param name="handler">The handler that contains the format and value providers.</param>
    /// <returns>A new instance of <see cref="ReferenceExpression"/> with the specified format and value providers.</returns>
    public static ReferenceExpression Create(in ExpressionInterpolatedStringHandler handler)
    {
        return handler.GetExpression();
    }
}

/// <summary>
/// Represents a handler for interpolated strings that contain expressions. Those expressions will either be literal strings or
/// instances of types that implement both <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
/// </summary>
/// <param name="literalLength">The length of the literal part of the interpolated string.</param>
/// <param name="formattedCount">The number of formatted parts in the interpolated string.</param>
[InterpolatedStringHandler]
public ref struct ExpressionInterpolatedStringHandler(int literalLength, int formattedCount)
{
    private readonly StringBuilder _builder = new(literalLength * 2);
    private readonly List<IValueProvider> _valueProviders = new(formattedCount);
    private readonly List<string> _manifestExpressions = new(formattedCount);

    /// <summary>
    /// Appends a literal value to the expression.
    /// </summary>
    /// <param name="value"></param>
    public readonly void AppendLiteral(string value)
    {
        _builder.Append(value);
    }

    /// <summary>
    /// Appends a formatted value to the expression.
    /// </summary>
    /// <param name="value"></param>
    public readonly void AppendFormatted(string? value)
    {
        _builder.Append(value);
    }

    /// <summary>
    /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
    /// </summary>
    /// <param name="valueProvider"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AppendFormatted<T>(T valueProvider) where T : IValueProvider, IManifestExpressionProvider
    {
        var index = _valueProviders.Count;
        _builder.Append(CultureInfo.InvariantCulture, $"{{{index}}}");

        _valueProviders.Add(valueProvider);
        _manifestExpressions.Add(valueProvider.ValueExpression);
    }

    internal readonly ReferenceExpression GetExpression() =>
        ReferenceExpression.Create(_builder.ToString(), [.. _valueProviders], [.. _manifestExpressions]);
}
