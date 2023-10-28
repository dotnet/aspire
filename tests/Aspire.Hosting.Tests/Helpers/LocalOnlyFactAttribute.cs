// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Helpers;

public class LocalOnlyFactAttribute : FactAttribute
{
    public override string Skip
    {
        get
        {
            if (Environment.GetEnvironmentVariable("BUILD_BUILDID") != null)
            {
                return "LocalOnlyFactAttribute tests are not run as part of CI.";
            }

            return null!;
        }

        set
        {
            // We ignore setting of skip value via attribute usage.
        }
    }
}
