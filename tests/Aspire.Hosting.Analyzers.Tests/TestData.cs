// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Analyzers.Tests;

internal static class TestData
{
    public sealed class InvalidModelNames : TheoryData<string>
    {
        public InvalidModelNames()
        {
            AddRange([
                "", // Can't be empty string
                " ", // Can't be just whitespace
                "no spaces allowed",
                "no_underscores_allowed",
                "no/slashes/thanks",
                "no\u20E0slashesu\u20E0thanks", // \u20E0 == backslash
                "can't-have-apostrophes",
                "no-special-chars&allowed",
                "no-special-chars$allowed",
                "no-special-chars#allowed",
                "no-special-chars*allowed",
                "1234-no-leading-numbers",
                "-no-leading-hyphens",
                "no-trailing-hyphens-",
                "no-consecutive--hyphens",
                "must-be-less-than-64-chars-long-must-be-less-than-64-chars-long-oops"
            ]);
        }
    }

    public sealed class ValidModelNames : TheoryData<string>
    {
        public ValidModelNames()
        {
            AddRange([
                "a", // Minimum length is 1
                "this-is-fine",
                "THIS-IS-FINE-TOO",
                "NoProblemWithThisName",
                "orthisone",
                "THISISALSOFINE",
                // Purposefully allows 5 more characters for a suffix for tests that need it
                "must-be-less-than-64-chars-long-must-be-less-than-64-chars"
            ]);
        }
    }
}
