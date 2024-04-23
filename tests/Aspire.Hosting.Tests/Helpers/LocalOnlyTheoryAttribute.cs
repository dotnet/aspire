// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Helpers;

public class LocalOnlyTheoryAttribute(params string[] executablesOnPath) : TheoryAttribute
{
    public override string Skip
    {
        get => LocalOnlyHelper.SkipIfRunningInCI(nameof(LocalOnlyTheoryAttribute), executablesOnPath);

        set
        {
            // We ignore setting of skip value via attribute usage.
        }
    }
}

