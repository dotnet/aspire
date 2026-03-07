// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a conditional value expression that selects between two <see cref="ReferenceExpression"/> branches
/// based on the string value of a condition.
/// </summary>
/// <remarks>
/// <para>
/// This type provides a declarative ternary-style expression that polyglot code generators can translate
/// into native conditional constructs (e.g., <c>condition ? trueVal : falseVal</c> in TypeScript,
/// <c>trueVal if condition else falseVal</c> in Python).
/// </para>
/// <para>
/// At runtime, the condition is evaluated and compared to <see cref="bool.TrueString"/>. If the condition
/// matches, the <see cref="WhenTrue"/> branch is resolved; otherwise the <see cref="WhenFalse"/> branch is used.
/// </para>
/// <para>
/// For the publish manifest, the conditional is resolved at publish time and a <c>value.v0</c> entry is
/// emitted. The <see cref="Name"/> is auto-generated from the condition's <see cref="IManifestExpressionProvider.ValueExpression"/>
/// and the <see cref="ValueExpression"/> references that entry using the <see cref="Name"/> property.
/// </para>
/// </remarks>
[DebuggerDisplay("ConditionalReferenceExpression = {ValueExpression}")]
[AspireExport(Description = "Represents an expression that evaluates to one of two branches based on the string value of a condition.", ExposeProperties = true)]
public class ConditionalReferenceExpression : IValueProvider, IManifestExpressionProvider, IValueWithReferences
{
    private static readonly ConcurrentDictionary<string, int> s_nameCounters = new(StringComparer.OrdinalIgnoreCase);

    private readonly IValueProvider _condition;

    /// <summary>
    /// Initializes a new instance of <see cref="ConditionalReferenceExpression"/>.
    /// </summary>
    /// <param name="condition">A value provider whose result is compared to <see cref="bool.TrueString"/>
    /// to determine which branch to evaluate. Typically an <see cref="EndpointReferenceExpression"/> with
    /// <see cref="EndpointProperty.TlsEnabled"/>.</param>
    /// <param name="whenTrue">The expression to evaluate when the condition is <see langword="true"/>.</param>
    /// <param name="whenFalse">The expression to evaluate when the condition is <see langword="false"/>.</param>
    private ConditionalReferenceExpression(IValueProvider condition, ReferenceExpression whenTrue, ReferenceExpression whenFalse)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(whenTrue);
        ArgumentNullException.ThrowIfNull(whenFalse);

        _condition = condition;
        WhenTrue = whenTrue;
        WhenFalse = whenFalse;
        Name = GenerateName(condition);
    }

    /// <summary>
    /// Creates a new <see cref="ConditionalReferenceExpression"/> with the specified condition and branch expressions.
    /// </summary>
    /// <param name="condition">A value provider whose result is compared to <see cref="bool.TrueString"/>.</param>
    /// <param name="whenTrue">The expression to evaluate when the condition is <see langword="true"/>.</param>
    /// <param name="whenFalse">The expression to evaluate when the condition is <see langword="false"/>.</param>
    /// <returns>A new <see cref="ConditionalReferenceExpression"/>.</returns>
    public static ConditionalReferenceExpression Create(IValueProvider condition, ReferenceExpression whenTrue, ReferenceExpression whenFalse)
    {
        return new ConditionalReferenceExpression(condition, whenTrue, whenFalse);
    }

    /// <summary>
    /// Gets the name of this conditional expression, used as the manifest resource name for the <c>value.v0</c> entry.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the condition value provider whose result is compared to <see cref="bool.TrueString"/>.
    /// </summary>
    public IValueProvider Condition => _condition;

    /// <summary>
    /// Gets the expression to evaluate when <see cref="Condition"/> evaluates to <see cref="bool.TrueString"/>.
    /// </summary>
    public ReferenceExpression WhenTrue { get; }

    /// <summary>
    /// Gets the expression to evaluate when <see cref="Condition"/> does not evaluate to <see cref="bool.TrueString"/>.
    /// </summary>
    public ReferenceExpression WhenFalse { get; }

    /// <summary>
    /// Gets the manifest expression that references the <c>value.v0</c> entry.
    /// </summary>
    public string ValueExpression => $"{{{Name}.value}}";

    /// <inheritdoc />
    public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        var conditionValue = await _condition.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var branch = string.Equals(conditionValue, bool.TrueString, StringComparison.OrdinalIgnoreCase) ? WhenTrue : WhenFalse;
        return await branch.GetValueAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken = default)
    {
        var conditionValue = await _condition.GetValueAsync(context, cancellationToken).ConfigureAwait(false);
        var branch = string.Equals(conditionValue, bool.TrueString, StringComparison.OrdinalIgnoreCase) ? WhenTrue : WhenFalse;
        return await branch.GetValueAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IEnumerable<object> References
    {
        get
        {
            if (_condition is IValueWithReferences conditionRefs)
            {
                foreach (var reference in conditionRefs.References)
                {
                    yield return reference;
                }
            }

            foreach (var reference in ((IValueWithReferences)WhenTrue).References)
            {
                yield return reference;
            }

            foreach (var reference in ((IValueWithReferences)WhenFalse).References)
            {
                yield return reference;
            }
        }
    }

    private static string GenerateName(IValueProvider condition)
    {
        string baseName;

        if (condition is IManifestExpressionProvider expressionProvider)
        {
            var expression = expressionProvider.ValueExpression;
            var sanitized = SanitizeExpression(expression);
            baseName = sanitized.Length > 0 ? $"cond-{sanitized}" : "cond-expr";
        }
        else
        {
            baseName = "cond-expr";
        }

        var count = s_nameCounters.AddOrUpdate(baseName, 1, (_, existing) => existing + 1);
        return count == 1 ? baseName : $"{baseName}-{count}";
    }

    private static string SanitizeExpression(string expression)
    {
        var builder = new StringBuilder(expression.Length);
        var lastWasSeparator = false;

        foreach (var ch in expression)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        return builder.ToString().TrimEnd('-');
    }
}
