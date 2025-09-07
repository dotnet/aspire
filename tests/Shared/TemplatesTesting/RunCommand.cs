// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Templates.Tests;

public class RunCommand : DotNetCommand
{
    public RunCommand(ITestOutputHelper _testOutput, BuildEnvironment? buildEnv = null, string label="") : base(_testOutput, false, buildEnv, label)
    {
        WithEnvironmentVariables(_buildEnvironment.EnvVars);
        WithEnvironmentVariable("DOTNET_ROOT", Path.GetDirectoryName(_buildEnvironment.DotNet)!);
        WithEnvironmentVariable("DOTNET_INSTALL_DIR", Path.GetDirectoryName(_buildEnvironment.DotNet)!);
        WithEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
        WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
    }
}
