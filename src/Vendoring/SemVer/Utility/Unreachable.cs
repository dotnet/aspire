using System;
using System.Diagnostics.CodeAnalysis;
using Semver.Parsing;

namespace Semver.Utility;

/// <summary>
/// Used to clearly mark when a case should be unreachable and helps properly manage code coverage.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Unreachable
{
    public static ArgumentException InvalidEnum(StandardOperator @operator)
        => new ArgumentException($"DEBUG: Invalid {nameof(StandardOperator)} value {@operator}.");

    public static ArgumentException InvalidEnum(WildcardVersion wildcardVersion)
        => new ArgumentException($"DEBUG: Invalid {nameof(WildcardVersion)} value {wildcardVersion}.");
}
