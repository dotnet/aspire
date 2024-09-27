// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Workload.Tests;

public class TemplatesCustomHive
{
    public string[] TemplatePackageIds { get; init; }

    private string? _customHiveDirectory;
    public string CustomHiveDirectory => _customHiveDirectory ?? throw new InvalidOperationException($"TemplatesCustomHive has not been installed yet for '{_customHiveDirName}'");
    private readonly string _customHiveDirName;

    private static readonly string s_tmpDirSuffix = Guid.NewGuid().ToString()[..8];

    // FIXME: these are not doing the install, so no need to be lazy!
    public static TemplatesCustomHive With9_0_Net9_And_Net8 => new(
            [
                TemplatePackageIdNames.AspireProjectTemplates_9_0_net9,
                TemplatePackageIdNames.AspireProjectTemplates_9_0_net8
            ], "templates-with-9-net8-net9");

    public static TemplatesCustomHive With9_0_Net8 { get; } = new([TemplatePackageIdNames.AspireProjectTemplates_9_0_net8], "templates-with-9-net8");

    public static TemplatesCustomHive With9_0_Net9 { get; } = new([TemplatePackageIdNames.AspireProjectTemplates_9_0_net9], "templates-with-9-net9");

    public TemplatesCustomHive(string[] templatePackageIds, string customHiveDirName)
    {
        TemplatePackageIds = templatePackageIds ?? throw new ArgumentNullException(nameof(templatePackageIds));

        ArgumentException.ThrowIfNullOrEmpty(customHiveDirName, nameof(customHiveDirName));
        _customHiveDirName = customHiveDirName;
    }

    public async Task InstallAsync(BuildEnvironment buildEnvironment)
    {
        var customHiveBaseDirectory = BuildEnvironment.IsRunningOnCI
                                        ? Path.Combine(Path.GetTempPath(), $"templates-${s_tmpDirSuffix}")
                                        : Path.Combine(AppContext.BaseDirectory, "templates");

        _customHiveDirectory = Path.Combine(customHiveBaseDirectory, _customHiveDirName);

        var packageIdAndPaths =
                TemplatePackageIds
                    .Select(id => GetPackagePath(buildEnvironment.BuiltNuGetsPath, id))
                    .Zip(TemplatePackageIds, (path, id) => (path, id));

        var installTemplates = true;
        if (Directory.Exists(CustomHiveDirectory))
        {
            if (BuildEnvironment.IsRunningOnCI)
            {
                installTemplates = false;
            }
            else
            {
                // local run, we can skip the installation if nothing has changed
                var dirWriteTime = Directory.GetLastWriteTimeUtc(CustomHiveDirectory);
                installTemplates = packageIdAndPaths.Where(t => new FileInfo(t.id).LastWriteTimeUtc > dirWriteTime).Any();
            }
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
        System.Console.WriteLine($"Looking for {templatePackageId}*.nupkg in {builtNuGetsPath}");
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
        // no cleanup on local
        // cleanup might.. um interfere with other uses??!@#
        // if (BuildEnvironment.IsRunningOnCI && Directory.Exists(CustomHiveDirectory))
        // {
        //     Directory.Delete(CustomHiveDirectory, recursive: true);
        // }
    }
}
