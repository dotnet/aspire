// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Templates.Tests;

public class DotNetCommand : ToolCommand
{
    protected readonly BuildEnvironment _buildEnvironment;
    private readonly bool _useDefaultArgs;

    public DotNetCommand(ITestOutputHelper _testOutput, bool useDefaultArgs = true, BuildEnvironment? buildEnv = null, string label = "")
            : base((buildEnv ?? BuildEnvironment.ForDefaultFramework).DotNet, _testOutput, label)
    {
        _buildEnvironment = buildEnv ?? BuildEnvironment.ForDefaultFramework;
        _useDefaultArgs = useDefaultArgs;
        if (useDefaultArgs)
        {
            WithEnvironmentVariables(_buildEnvironment.EnvVars);
        }
    }

    protected override string GetFullArgs(params string[] args)
        => _useDefaultArgs
                ? $"{string.Join(" ", args)} {_buildEnvironment.DefaultBuildArgs}"
                : string.Join(" ", args);
}
