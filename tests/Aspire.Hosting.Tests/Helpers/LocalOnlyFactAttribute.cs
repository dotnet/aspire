// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Helpers;

public class LocalOnlyFactAttribute(params string[] executablesOnPath) : FactAttribute
{
    public override string Skip
    {
        get => LocalOnlyHelper.SkipIfRunningInCI(nameof(LocalOnlyFactAttribute), executablesOnPath);

        set
        {
            // We ignore setting of skip value via attribute usage.
        }
    }
}

