using System.Collections.Generic;

namespace Semver.Utility;

/// <summary>
/// Internal helper for efficiently creating empty read only lists
/// </summary>
internal static class ReadOnlyList<T>
{
    public static readonly IReadOnlyList<T> Empty = new List<T>().AsReadOnly();
}
