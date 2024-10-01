// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Workload.Tests;

public class TemplatesCustomHive
{
    private static readonly string s_tmpDirSuffix = Guid.NewGuid().ToString()[..8];

    private readonly string _customHiveDirectory;
    private readonly string _customHiveDirName;

    public static TemplatesCustomHive With9_0_Net8 { get; } = new([TemplatePackageIdNames.AspireProjectTemplates_9_0_net8], "templates-with-9-net8");

    public static TemplatesCustomHive With9_0_Net9 { get; } = new([TemplatePackageIdNames.AspireProjectTemplates_9_0_net9], "templates-with-9-net9");
    public static TemplatesCustomHive With9_0_Net9_And_Net8 => new(
            [
                TemplatePackageIdNames.AspireProjectTemplates_9_0_net9,
                TemplatePackageIdNames.AspireProjectTemplates_9_0_net8
            ], "templates-with-9-net8-net9");

    public string[] TemplatePackageIds { get; init; }
    public string CustomHiveDirectory => _customHiveDirectory ?? throw new InvalidOperationException($"TemplatesCustomHive has not been installed yet for '{_customHiveDirName}'");

    public TemplatesCustomHive(string[] templatePackageIds, string customHiveDirName)
    {
        TemplatePackageIds = templatePackageIds ?? throw new ArgumentNullException(nameof(templatePackageIds));

        ArgumentException.ThrowIfNullOrEmpty(customHiveDirName, nameof(customHiveDirName));
        _customHiveDirName = customHiveDirName;
        var customHiveBaseDirectory = BuildEnvironment.IsRunningOnCI
                                        ? Path.Combine(Path.GetTempPath(), $"templates-${s_tmpDirSuffix}")
                                        : Path.Combine(AppContext.BaseDirectory, "templates");
        _customHiveDirectory = Path.Combine(customHiveBaseDirectory, _customHiveDirName);
    }

    public async Task EnsureInstalledAsync(BuildEnvironment buildEnvironment)
    {
        if (BuildEnvironment.IsRunningOnCI && Directory.Exists(_customHiveDirectory))
        {
            // nothing to do
            Console.WriteLine($"** Custom hive exists, skipping installation: {_customHiveDirectory}");
            return;
        }

        // For local runs, we can skip the installation if nothing has changed
        var packageIdAndPaths =
                TemplatePackageIds
                    .Select(id => GetPackagePath(buildEnvironment.BuiltNuGetsPath, id))
                    .Zip(TemplatePackageIds, (path, id) => (path, id));

        var installTemplates = true;
        if (Directory.Exists(CustomHiveDirectory))
        {
            // local run, we can skip the installation if nothing has changed
            var dirWriteTime = Directory.GetLastWriteTimeUtc(CustomHiveDirectory);
            installTemplates = packageIdAndPaths.Where(t => new FileInfo(t.id).LastWriteTimeUtc > dirWriteTime).Any();
        }

        if (!installTemplates)
        {
            Console.WriteLine($"** Custom hive exists, skipping installation: {CustomHiveDirectory}");
            return;
        }

        if (Directory.Exists(CustomHiveDirectory))
        {
            Directory.Delete(CustomHiveDirectory, recursive: true);
        }

        Console.WriteLine($"*** Creating templates custom hive: {CustomHiveDirectory}");
        Directory.CreateDirectory(CustomHiveDirectory);

        foreach (var (packagePath, templatePackageId) in packageIdAndPaths)
        {
            await InstallTemplatesAsync(
                    packagePath,
                    customHiveDirectory: _customHiveDirectory,
                    dotnet: buildEnvironment.DotNet);
        }
    }

    public static string GetPackagePath(string builtNuGetsPath, string templatePackageId)
    {
        var packages = Directory.EnumerateFiles(builtNuGetsPath, $"{templatePackageId}*.nupkg");
        if (!packages.Any())
        {
            throw new ArgumentException($"Cannot find {templatePackageId}*.nupkg in {builtNuGetsPath}. Found packages: {string.Join(", ", Directory.EnumerateFiles(builtNuGetsPath))}");
        }
        if (packages.Count() > 1)
        {
            throw new ArgumentException($"Found more than one {templatePackageId}*.nupkg in {builtNuGetsPath}: {string.Join(", ", packages)}");
        }
        return packages.Single();
    }

    public static async Task<CommandResult> InstallTemplatesAsync(string packagePath, string customHiveDirectory, string dotnet)
    {
        var installCmd = $"new install --debug:custom-hive {customHiveDirectory} {packagePath}";
        using var cmd = new ToolCommand(dotnet,
                                        new TestOutputWrapper(forceShowBuildOutput: true),
                                        label: "template install");

        var res = await cmd.ExecuteAsync(installCmd);
        res.EnsureSuccessful();
        return res;
    }

    public void Cleanup()
    {
    }
}
