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
public class ReferenceExpression : IManifestExpressionProvider, IValueProvider, IValueWithReferences
{
    private readonly IValueProvider[] _valueProviders;
    private readonly string[] _manifestExpressions;

    private ReferenceExpression(string format, IValueProvider[] valueProviders, string[] manifestExpressions)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(valueProviders);
        ArgumentNullException.ThrowIfNull(manifestExpressions);

        Format = format;
        _valueProviders = valueProviders;
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
    public IReadOnlyList<IValueProvider> ValueProviders => _valueProviders;

    /// <summary>
    /// A delegate that will be used to escape values provided from <see cref="ValueProviders"/> when <see cref="GetValueAsync(CancellationToken)"/> is called.
    /// </summary>
    public Func<string?, string?>? EscapeValue { get; set; }

    IEnumerable<object> IValueWithReferences.References => ValueProviders;

    /// <summary>
    /// The value expression for the format string.
    /// </summary>
    public string ValueExpression =>
        string.Format(CultureInfo.InvariantCulture, Format, _manifestExpressions);

    /// <summary>
    /// Gets the value of the expression. The final string value after evaluating the format string and its parameters.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
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
            var value = await ValueProviders[i].GetValueAsync(cancellationToken).ConfigureAwait(false);
            args[i] = EscapeValue is not null ? EscapeValue(value) : value;
        }

        return string.Format(CultureInfo.InvariantCulture, Format, args);
    }

    /// <summary>
    /// Creates a new instance of this <see cref="ReferenceExpression"/> for use within a URI.
    /// </summary>
    /// <remarks>
    /// Values from <see cref="ValueProviders"/> will be escaped by calling <see cref="Uri.EscapeDataString(string)"/>.
    /// </remarks>
    /// <returns>A new instance of this <see cref="ReferenceExpression"/> for use within a URI.</returns>
    public ReferenceExpression ForUri() => Create(Format, _valueProviders, _manifestExpressions, UriEscapeDataString);

    internal static ReferenceExpression Create(string format, IValueProvider[] valueProviders, string[] manifestExpressions, Func<string?, string?>? escapeValue = null)
    {
        return new(format, valueProviders, manifestExpressions) { EscapeValue = escapeValue };
    }

    /// <summary>
    /// Creates a new instance of <see cref="ReferenceExpression"/> with the specified format and value providers.
    /// </summary>
    /// <param name="handler">The handler that contains the format and value providers.</param>
    /// <param name="escapeValue">An optional delegate that will be used to escape the values.</param>
    /// <returns>A new instance of <see cref="ReferenceExpression"/> with the specified format and value providers.</returns>
    public static ReferenceExpression Create(in ExpressionInterpolatedStringHandler handler, Func<string?, string?>? escapeValue = null)
    {
        var expression = handler.GetExpression();
        expression.EscapeValue = escapeValue;
        return expression;
    }

    private static string? UriEscapeDataString(string? stringToEscape)
    {
        return stringToEscape is not null ? Uri.EscapeDataString(stringToEscape) : stringToEscape;
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
    /// <param name="value">The literal string value to be appended to the interpolated string.</param>
    public readonly void AppendLiteral(string value)
    {
        _builder.Append(value);
    }

    /// <summary>
    /// Appends a formatted value to the expression.
    /// </summary>
    /// <param name="value">The formatted string to be appended to the interpolated string.</param>
    public readonly void AppendFormatted(string? value)
    {
        _builder.Append(value);
    }

    /// <summary>
    /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
    /// </summary>
    /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
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
