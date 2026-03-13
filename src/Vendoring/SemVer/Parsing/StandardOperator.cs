namespace Semver.Parsing;

internal enum StandardOperator
{
    None = 1,
    Equals,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    /// <summary>
    /// Approximately equivalent to version. Allows patch updates.
    /// </summary>
    Tilde,
    /// <summary>
    /// Compatible with version. Allows minor and patch updates.
    /// </summary>
    Caret,
}
