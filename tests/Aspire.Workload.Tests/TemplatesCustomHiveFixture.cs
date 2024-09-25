// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// FIXME: rename to CustomHive or something?
using Aspire.Workload.Tests;
using Xunit;

public class TemplatesCustomHiveFixture : IAsyncLifetime
{
    // FIXME: rename
    public string HomeDirectory { get; set; }
    private readonly TestOutputWrapper _testOutput;
    private readonly string _templatesPackageId;

    // FIXME: pass the BE also for tfm
    public TemplatesCustomHiveFixture(string templatesPackageId, string? tempDirName = null)
    {
        HomeDirectory = Path.Combine(Path.GetTempPath(), tempDirName ?? Guid.NewGuid().ToString());
        _templatesPackageId = templatesPackageId;
        // FIXME: use alwaysstdout=true
        _testOutput = new TestOutputWrapper(forceShowBuildOutput: true);
    }

    public async Task InitializeAsync()
    {
        _testOutput.WriteLine($"Creating HomeDirectory: {HomeDirectory}");
        Directory.CreateDirectory(HomeDirectory);

        using var cmd = new DotNetNewCommand(_testOutput, label: "dotnet-new-install")
                            .WithCustomHive(HomeDirectory);
        var res = await cmd.ExecuteAsync($"install {Path.Combine(BuildEnvironment.ForDefaultFramework.BuiltNuGetsPath, _templatesPackageId + ".9.0.0-dev.nupkg")}");
        res.EnsureSuccessful();
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(HomeDirectory))
        {
            Directory.Delete(HomeDirectory, recursive: true);
        }
        return Task.CompletedTask;
    }
}
