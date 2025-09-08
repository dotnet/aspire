// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.TestUtilities;

/// <summary>
/// Marks a test as "outerloop" so that it runs in the outerloop CI but not in regular CI.
/// </summary>
/// <remarks>
/// <para>
/// This attribute works by applying xUnit.net "Traits" based on the criteria specified in the attribute
/// properties. Once these traits are applied, build scripts can include/exclude tests based on them.
/// </para>
/// <example>
/// <code>
/// [Fact]
/// [OuterloopTest]
/// public void LongRunningTest()
/// {
///     // Long running test
/// }
/// </code>
///
/// <para>
/// The above example generates the following facet:
/// </para>
///
/// <list type="bullet">
/// <item>
///     <description><c>outerloop</c> = <c>true</c></description>
/// </item>
/// </list>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class OuterloopTestAttribute : Attribute, ITraitAttribute
{
    /// <summary>
    /// Gets an optional reason for marking this test as outerloop, such as a description of why it needs to run in outerloop.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OuterloopTestAttribute"/> class with an optional <see cref="Reason"/>.
    /// </summary>
    /// <param name="reason">A reason that this test is marked as outerloop.</param>
    public OuterloopTestAttribute(string? reason = null)
    {
        Reason = reason;
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new KeyValuePair<string, string>("outerloop", "true")];
}