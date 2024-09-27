// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class DotNetNewCommand : DotNetCommand
{
    private string? _customHive;

    public DotNetNewCommand(ITestOutputHelper _testOutput, bool useDefaultArgs = true, BuildEnvironment? buildEnv = null, string label = "dotnet-new")
            : base(_testOutput, useDefaultArgs, buildEnv, label)
    {
        WithCustomHive(_buildEnvironment.TemplatesHomeDirectory);
    }

    public DotNetNewCommand WithCustomHive(string hiveDirectory)
    {
        _customHive = hiveDirectory;
        return this;
    }

    protected override string GetFullArgs(params string[] args)
        => $"new {base.GetFullArgs(args)}"
                + (_customHive is not null ? $" --debug:custom-hive \"{_customHive}\"" : "");
}
