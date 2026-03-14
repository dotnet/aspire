using System.Collections.Generic;

namespace Semver.Parsing;

internal static class PrereleaseIdentifiers
{
    public static readonly IReadOnlyList<PrereleaseIdentifier> Zero
        = new List<PrereleaseIdentifier>(1) { PrereleaseIdentifier.Zero }.AsReadOnly();
}
