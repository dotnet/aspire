// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an expression that might be made up of multiple resource properties. For example,
/// a connection string might be made up of a host, port, and password from different endpoints.
/// </summary>
public class ReferenceExpression : IManifestExpressionProvider, IValueProvider, IValueWithReferences
{
    /// <summary>
    /// Represents an empty reference expression with no name, value providers, or arguments.
    /// </summary>
    /// <remarks>Use this field to represent a default or uninitialized reference expression. The instance has
    /// an empty name and contains no value providers or arguments.</remarks>
    public static readonly ReferenceExpression Empty = Create(string.Empty, [], [], []);

    private readonly string[] _manifestExpressions;
    private readonly string?[] _stringFormats;

    private ReferenceExpression(string format, IValueProvider[] valueProviders, string[] manifestExpressions, string?[] stringFormats)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(valueProviders);
        ArgumentNullException.ThrowIfNull(manifestExpressions);

        Format = format;
        ValueProviders = valueProviders;
        _manifestExpressions = manifestExpressions;
        _stringFormats = stringFormats;
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
    /// The string formats of the parameters, e.g. "uri".
    /// </summary>
    public IReadOnlyList<string?> StringFormats => _stringFormats;

    /// <summary>
    /// The list of <see cref="IValueProvider"/> that will be used to resolve parameters for the format string.
    /// </summary>
    public IReadOnlyList<IValueProvider> ValueProviders { get; }

    IEnumerable<object> IValueWithReferences.References => ValueProviders;

    /// <summary>
    /// The value expression for the format string.
    /// </summary>
    public string ValueExpression =>
        string.Format(CultureInfo.InvariantCulture, Format, _manifestExpressions);

    /// <summary>
    /// Gets the value of the expression. The final string value after evaluating the format string and its parameters.
    /// </summary>
    /// <param name="context">A context for resolving the value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public async ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        // NOTE: any logical changes to this method should also be made to ExpressionResolver.EvalExpressionAsync
        if (Format.Length == 0)
        {
            return null;
        }

        var args = new object?[ValueProviders.Count];
        for (var i = 0; i < ValueProviders.Count; i++)
        {
            args[i] = await ValueProviders[i].GetValueAsync(context, cancellationToken).ConfigureAwait(false);

            // Apply string format if needed
            var stringFormat = _stringFormats[i];
            if (stringFormat is not null && args[i] is string s)
            {
                args[i] = FormattingHelpers.FormatValue(s, stringFormat);
            }
        }

        return string.Format(CultureInfo.InvariantCulture, Format, args);
    }

    /// <summary>
    /// Gets the value of the expression. The final string value after evaluating the format string and its parameters.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken)
    {
        return this.GetValueAsync(new ValueProviderContext(), cancellationToken);
    }

    internal static ReferenceExpression Create(string format, IValueProvider[] valueProviders, string[] manifestExpressions, string?[] stringFormats)
    {
        return new(format, valueProviders, manifestExpressions, stringFormats);
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

    /// <summary>
    /// Represents a handler for interpolated strings that contain expressions. Those expressions will either be literal strings or
    /// instances of types that implement both <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
    /// </summary>
    /// <param name="literalLength">The length of the literal part of the interpolated string.</param>
    /// <param name="formattedCount">The number of formatted parts in the interpolated string.</param>
    [InterpolatedStringHandler]
    public ref struct ExpressionInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        private static readonly char[] s_braces = ['{', '}'];
        private readonly StringBuilder _builder = new(literalLength * 2);
        private readonly List<IValueProvider> _valueProviders = new(formattedCount);
        private readonly List<string> _manifestExpressions = new(formattedCount);
        private readonly List<string?> _stringFormats = new(formattedCount);

        /// <summary>
        /// Appends a literal value to the expression.
        /// </summary>
        /// <param name="value">The literal string value to be appended to the interpolated string.</param>
        public readonly void AppendLiteral(string value)
        {
            // Only escape single braces, leave already escaped braces untouched
            _builder.Append(EscapeUnescapedBraces(value));
        }

        /// <summary>
        /// Appends a formatted value to the expression.
        /// </summary>
        /// <param name="value">The formatted string to be appended to the interpolated string.</param>
        public readonly void AppendFormatted(string? value)
        {
            AppendFormatted(value, format: null);
        }

        /// <summary>
        /// Appends a formatted value to the expression.
        /// </summary>
        /// <param name="value">The formatted string to be appended to the interpolated string.</param>
        /// <param name="format">The format to be applied to the value. e.g., "uri"</param>
        public readonly void AppendFormatted(string? value, string? format = null)
        {
            // The value that comes in is a literal string that is not meant to be interpreted.
            // But the _builder later gets treated as a format string, so we just need to escape the braces.
            if (value is not null)
            {
                if (format is not null)
                {
                    value = FormattingHelpers.FormatValue(value, format);
                }

                _builder.Append(EscapeUnescapedBraces(value));
            }
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(T valueProvider) where T : IValueProvider, IManifestExpressionProvider
        {
            AppendFormatted(valueProvider, format: null);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <param name="format">The format to be applied to the value. e.g., "uri"</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(T valueProvider, string? format = null) where T : IValueProvider, IManifestExpressionProvider
        {
            var index = _valueProviders.Count;
            _builder.Append(CultureInfo.InvariantCulture, $"{{{index}}}");

            _valueProviders.Add(valueProvider);
            _manifestExpressions.Add(valueProvider.ValueExpression);
            _stringFormats.Add(format);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(IResourceBuilder<T> valueProvider)
            where T : IResource, IValueProvider, IManifestExpressionProvider
        {
            AppendFormatted(valueProvider, format: null);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <param name="format">The format to be applied to the value. e.g., "uri"</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(IResourceBuilder<T> valueProvider, string? format = null)
            where T : IResource, IValueProvider, IManifestExpressionProvider
        {
            var index = _valueProviders.Count;
            _builder.Append(CultureInfo.InvariantCulture, $"{{{index}}}");

            _valueProviders.Add(valueProvider.Resource);
            _manifestExpressions.Add(valueProvider.Resource.ValueExpression);
            _stringFormats.Add(format);
        }

        internal readonly ReferenceExpression GetExpression() =>
            new(_builder.ToString(), [.. _valueProviders], [.. _manifestExpressions], [.. _stringFormats]);

        private static string EscapeUnescapedBraces(string input)
        {
            // Fast path: nothing to escape
            if (input.IndexOfAny(s_braces) == -1)
            {
                return input;
            }

            // Allocate a bit of extra space in case we need to escape a few braces.
            var sb = new StringBuilder(input.Length + 4);

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (IsBrace(c))
                {
                    if (IsNextCharSame(input, i))
                    {
                        // Already escaped, copy both and skip next
                        sb.Append(c).Append(c);
                        i++;
                    }
                    else
                    {
                        // Escape single brace
                        sb.Append(c).Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();

            static bool IsBrace(char ch) => ch == '{' || ch == '}';
            static bool IsNextCharSame(string s, int idx) =>
                idx + 1 < s.Length && s[idx + 1] == s[idx];
        }
    }
}

/// <summary>
/// A builder for creating <see cref="ReferenceExpression"/> instances.
/// </summary>
public class ReferenceExpressionBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly List<IValueProvider> _valueProviders = new();
    private readonly List<string> _manifestExpressions = new();
    private readonly List<string?> _stringFormats = new();

    /// <summary>
    /// Indicates whether the expression is empty.
    /// </summary>
    public bool IsEmpty => _builder.Length == 0;

    /// <summary>
    /// Appends an interpolated string to the expression.
    /// </summary>
    /// <param name="handler"></param>
    public void Append([InterpolatedStringHandlerArgument("")] in ReferenceExpressionBuilderInterpolatedStringHandler handler)
    {
    }

    /// <summary>
    /// Appends a literal value to the expression.
    /// </summary>
    /// <param name="value">The literal string value to be appended to the interpolated string.</param>
    public void AppendLiteral(string value)
    {
        _builder.Append(value);
    }

    /// <summary>
    /// Appends a formatted value to the expression.
    /// </summary>
    /// <param name="value">The formatted string to be appended to the interpolated string.</param>
    public void AppendFormatted(string? value)
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
        AppendFormatted(valueProvider, format: null);
    }

    /// <summary>
    /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
    /// </summary>
    /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
    /// <param name="format">The format to be applied to the value. e.g., "uri"</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AppendFormatted<T>(T valueProvider, string? format) where T : IValueProvider, IManifestExpressionProvider
    {
        var index = _valueProviders.Count;
        _builder.Append(CultureInfo.InvariantCulture, $"{{{index}}}");

        _valueProviders.Add(valueProvider);
        _manifestExpressions.Add(valueProvider.ValueExpression);
        _stringFormats.Add(format);
    }

    /// <summary>
    /// Builds the <see cref="ReferenceExpression"/>.
    /// </summary>
    public ReferenceExpression Build() =>
        ReferenceExpression.Create(_builder.ToString(), [.. _valueProviders], [.. _manifestExpressions], [.. _stringFormats]);

    /// <summary>
    /// Represents a handler for interpolated strings that contain expressions. Those expressions will either be literal strings or
    /// instances of types that implement both <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
    /// </summary>
    /// <param name="literalLength">The length of the literal part of the interpolated string.</param>
    /// <param name="formattedCount">The number of formatted parts in the interpolated string.</param>
    /// <param name="builder">The builder that will be used to create the <see cref="ReferenceExpression"/>.</param>
    [InterpolatedStringHandler]
#pragma warning disable CS9113 // Parameter is unread.
    public ref struct ReferenceExpressionBuilderInterpolatedStringHandler(int literalLength, int formattedCount, ReferenceExpressionBuilder builder)
#pragma warning restore CS9113 // Parameter is unread.
    {
        /// <summary>
        /// Appends a literal value to the expression.
        /// </summary>
        /// <param name="value">The literal string value to be appended to the interpolated string.</param>
        public readonly void AppendLiteral(string value)
        {
            builder.AppendLiteral(value);
        }

        /// <summary>
        /// Appends a formatted value to the expression.
        /// </summary>
        /// <param name="value">The formatted string to be appended to the interpolated string.</param>
        public readonly void AppendFormatted(string? value)
        {
            builder.AppendFormatted(value);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(T valueProvider) where T : IValueProvider, IManifestExpressionProvider
        {
            AppendFormatted(valueProvider, format: null);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <param name="format">The format to be applied to the value. e.g., "uri"</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(T valueProvider, string? format) where T : IValueProvider, IManifestExpressionProvider
        {
            builder.AppendFormatted(valueProvider, format);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(IResourceBuilder<T> valueProvider)
            where T : IResource, IValueProvider, IManifestExpressionProvider
        {
            AppendFormatted(valueProvider, format: null);
        }

        /// <summary>
        /// Appends a formatted value to the expression. The value must implement <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.
        /// </summary>
        /// <param name="valueProvider">An instance of an object which implements <see cref="IValueProvider"/> and <see cref="IManifestExpressionProvider"/>.</param>
        /// <param name="format">The format to be applied to the value. e.g., "uri"</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AppendFormatted<T>(IResourceBuilder<T> valueProvider, string? format)
            where T : IResource, IValueProvider, IManifestExpressionProvider
        {
            builder.AppendFormatted(valueProvider.Resource, format);
        }
    }
}
