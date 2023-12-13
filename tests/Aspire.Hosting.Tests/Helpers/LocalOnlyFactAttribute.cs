// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests.Helpers;

public class LocalOnlyFactAttribute(params string[] executablesOnPath) : FactAttribute
{
    public override string Skip
    {
        get
        {
            // BUILD_BUILDID is defined by Azure Dev Ops

            if (Environment.GetEnvironmentVariable("BUILD_BUILDID") != null)
            {
                return "LocalOnlyFactAttribute tests are not run as part of CI.";
            }

            foreach (var executable in executablesOnPath)
            {
                if (FileUtil.FindFullPathFromPath(executable) is null)
                {
                    return $"Unable to locate {executable} on the PATH";
                }
            }

            return null!;
        }

        set
        {
            // We ignore setting of skip value via attribute usage.
        }
    }
}

