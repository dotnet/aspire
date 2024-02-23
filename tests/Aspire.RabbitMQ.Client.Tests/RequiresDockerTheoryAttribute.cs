// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.RabbitMQ.Client.Tests;

// TODO: remove these attributes when Helix has a Windows agent with Docker support
public class RequiresDockerTheoryAttribute : TheoryAttribute
{
    public static bool IsSupported => !OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is null;

    public override string Skip
    {
        get
        {
            if (!IsSupported)
            {
                return "RequiresDockerFactAttribute tests are not run on Windows during CI.";
            }

            return null!;
        }
    }
}

public class RequiresDockerFactAttribute : FactAttribute
{
    public override string Skip
    {
        get
        {
            if (!RequiresDockerTheoryAttribute.IsSupported)
            {
                return "RequiresDockerFactAttribute tests are not run on Windows during CI.";
            }

            return null!;
        }
    }
}
