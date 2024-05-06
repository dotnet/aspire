// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Helpers;

public class SkipOnHelixTheoryAttribute() : TheoryAttribute
{
    public override string Skip
    {
        get
        {
            if (Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null)
            {
                return "This is case is skipped on Helix.";
            }

            return null!;
        }

        set
        {
            // We ignore setting of skip value via attribute usage.
        }
    }
}

