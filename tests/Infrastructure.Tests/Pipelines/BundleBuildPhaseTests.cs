// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Xml.Linq;
using Xunit;

namespace Infrastructure.Tests;

public sealed class BundleBuildPhaseTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("linux-x64", false)]
    [InlineData("linux-arm64", true)]
    [InlineData("osx-arm64", false)]
    public async Task PackagingPreservesPreparedBytesWithoutPublishing(string rid, bool packDcp)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.CreateDirectory("bundle inputs with spaces").FullName;
        var project = CreateProject(root, rid);

        await RunAsync(project, "PublishBundleComponents", rid, "SkipNativeBuild=true", "SuppressAOT=true");
        var dashboard = DashboardPath(root, rid);
        File.WriteAllText(dashboard, "signed Dashboard");
        await RunAsync(project, "PackageBundlePayload", rid, $"PackDcpPackage={packDcp}", "SkipNativeBuild=true", "SuppressAOT=true");

        Assert.Equal("signed Dashboard", File.ReadAllText(dashboard));
        Assert.Equal("signed Dashboard", File.ReadAllText(Path.Combine(root, "package-dashboard")));
        Assert.Equal("signed Dashboard", File.ReadAllText(Path.Combine(root, "layout-dashboard")));
        var expected = new List<string> { "managed", "dashboard", "terminalhost" };
        if (rid.StartsWith("osx-", StringComparison.Ordinal))
        {
            expected.Add("tray");
        }
        expected.Add("package");
        if (packDcp)
        {
            expected.Add("dcp-package");
        }
        expected.AddRange(["restore", "layout"]);
        if (rid.StartsWith("osx-", StringComparison.Ordinal))
        {
            expected.Add("verify");
        }
        Assert.Equal(expected,
            File.ReadAllLines(Path.Combine(root, "events")).Select(line => line.Split('|')[0]));

        foreach (var line in File.ReadAllLines(Path.Combine(root, "properties")))
        {
            var values = line.Split('|');
            var runtimeIdentifier = values[0] is "managed" or "dashboard" or "terminalhost" or "tray" ? rid : "";
            Assert.Equal(["Release", rid, "true", "42.0.0", "ci.test", "20261014.1", "staging", "release", "true", "true", "", "", runtimeIdentifier], values[1..]);
        }
        Assert.Equal(rid, File.ReadAllText(Path.Combine(root, "dashboard-platform")).Trim());
        Assert.Equal("true", File.ReadAllText(Path.Combine(root, "package-skip-publish")).Trim());
        Assert.Contains("shared Dashboard publish warning",
            File.ReadAllText(Path.Combine(root, "artifacts", "log", "Release", "PublishNativeDashboard.log")));
    }

    [Theory]
    [InlineData("PublishBundleComponents")]
    [InlineData("PackageBundlePayload")]
    public async Task ExplicitPhasesRejectCiDebugBeforeDoingWork(string target)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, "linux-x64");

        var result = await ExecuteAsync(project, target, "linux-x64", "Configuration=Debug");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("CI native bundles must use Configuration=Release.", result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("win-x64:win-arm64")]
    [InlineData("win-x64;win-arm64")]
    [InlineData("win-x86")]
    public async Task ExplicitPhasesRejectUnsupportedOrMultipleRids(string rid)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, "linux-x64");

        foreach (var target in new[] { "PublishBundleComponents", "PackageBundlePayload" })
        {
            var result = await ExecuteAsync(project, target, rid);
            Assert.NotEqual(0, result.ExitCode);
        }
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("managed")]
    [InlineData("dashboard")]
    [InlineData("terminalhost")]
    [InlineData("script")]
    [InlineData("sqlite")]
    public async Task PackagingFailsForMissingPreparedInputRatherThanRepublishing(string missing)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        var file = missing switch
        {
            "managed" => ManagedPath(workspace.Path, rid),
            "dashboard" => DashboardPath(workspace.Path, rid),
            "terminalhost" => TerminalHostPath(workspace.Path, rid),
            "script" => Path.Combine(Path.GetDirectoryName(DashboardPath(workspace.Path, rid))!, "wwwroot", "_framework", "blazor.web.js"),
            _ => Path.Combine(Path.GetDirectoryName(DashboardPath(workspace.Path, rid))!, "libe_sqlite3.so")
        };
        File.Delete(file);
        var before = File.ReadAllLines(Path.Combine(workspace.Path, "events"));

        var result = await ExecuteAsync(project, "PackageBundlePayload", rid);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Prepared bundle input is missing:", result.Output);
        Assert.Equal(before, File.ReadAllLines(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("win-x64")]
    [InlineData("win-arm64")]
    public async Task WindowsPackagingConsumesSignedPreparedFilesWithoutPublishing(string rid)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.Path;
        var project = CreateProject(root, rid);
        var dashboardDirectory = Path.GetDirectoryName(DashboardPath(root, rid))!;
        var tray = Path.Combine(root, "artifacts", "tray", "aspire-tray.exe");
        foreach (var file in new[]
        {
            ManagedPath(root, rid), DashboardPath(root, rid), TerminalHostPath(root, rid),
            Path.Combine(dashboardDirectory, "wwwroot", "_framework", "blazor.web.js"),
            Path.Combine(dashboardDirectory, "e_sqlite3.dll"), tray,
            Path.Combine(root, "artifacts", "tray", "Aspire.ico")
        })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "signed prepared bytes");
        }

        await RunAsync(project, "PackageBundlePayload", rid, "SkipNativeBuild=true");

        Assert.Equal(["package", "restore", "layout", "verify"],
            File.ReadAllLines(Path.Combine(root, "events")));
        Assert.Equal("signed prepared bytes", File.ReadAllText(Path.Combine(root, "package-dashboard")));
        Assert.Equal("signed prepared bytes", File.ReadAllText(Path.Combine(root, "layout-dashboard")));
        Assert.Equal("signed prepared bytes", File.ReadAllText(tray));
        Assert.Equal("signed prepared bytes", File.ReadAllText(Path.Combine(root, "layout-tray")));

        File.Delete(tray);
        var before = File.ReadAllLines(Path.Combine(root, "events"));
        var result = await ExecuteAsync(project, "PackageBundlePayload", rid);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Prepared bundle input is missing:", result.Output);
        Assert.Equal(before, File.ReadAllLines(Path.Combine(root, "events")));
    }

    [Fact]
    public async Task StandaloneBuildRetainsSkipOptionsWithoutProducingPackages()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "Build", rid, "SkipNativeBuild=true");
        Assert.Equal(["managed", "dashboard", "terminalhost", "restore", "layout"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")).Select(line => line.Split('|')[0]));

        File.Delete(Path.Combine(workspace.Path, "events"));
        await RunAsync(project, "Build", rid, "SkipManagedBuild=true", "SkipNativeBuild=true", "SkipTrayBuild=true");
        Assert.Equal(["restore", "layout"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")).Select(line => line.Split('|')[0]));
    }

    [Fact]
    public async Task NativeCliSkipStillPublishesTrayAndTraySkipReusesIt()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "Build", rid, "SkipNativeBuild=true");
        Assert.Equal(["managed", "dashboard", "terminalhost", "tray", "restore", "layout", "verify"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));

        File.Delete(Path.Combine(workspace.Path, "events"));
        await RunAsync(project, "Build", rid, "SkipManagedBuild=true", "SkipNativeBuild=true", "SkipTrayBuild=true");
        Assert.Equal(["restore", "layout", "verify"], File.ReadAllLines(Path.Combine(workspace.Path, "events")));
    }

    private static string CreateProject(string root, string rid)
    {
        var events = Path.Combine(root, "events");
        var properties = Path.Combine(root, "properties");
        var propertyRecord = "$(Configuration)|$(TargetRid)|$(ContinuousIntegrationBuild)|$(VersionPrefix)|$(VersionSuffix)|$(OfficialBuildId)|$(AspireCliChannel)|$(DotNetFinalVersionKind)|$(StabilizePackageVersion)|$(IsOfficialBuild)|$(SkipNativeBuild)|$(SuppressAOT)|$(RuntimeIdentifier)";
        var managed = Path.Combine(root, "managed.proj");
        var dashboard = Path.Combine(root, "dashboard.proj");
        var terminal = Path.Combine(root, "terminal.proj");
        var package = Path.Combine(root, "package.proj");
        var dcp = Path.Combine(root, "dcp.proj");
        var apphost = Path.Combine(root, "apphost.proj");
        var tray = Path.Combine(root, "tray.proj");
        var trayApp = Path.Combine(root, "artifacts", "tray", "Aspire Tray.app");
        var dashboardDirectory = Path.GetDirectoryName(DashboardPath(root, rid))!;

        WriteChild(managed, "Publish", "managed", ManagedPath(root, rid));
        WriteChild(dashboard, "Publish", "dashboard", DashboardPath(root, rid),
            new XElement("Warning", new XAttribute("Text", "shared Dashboard publish warning")),
            Write(Path.Combine(root, "dashboard-platform"), "$(PlatformName)"),
            Write(Path.Combine(dashboardDirectory, "wwwroot", "_framework", "blazor.web.js"), "Blazor"),
            Write(Path.Combine(dashboardDirectory, rid.StartsWith("osx-", StringComparison.Ordinal) ? "libe_sqlite3.dylib"
                : rid.StartsWith("win-", StringComparison.Ordinal) ? "e_sqlite3.dll" : "libe_sqlite3.so"), "SQLite"));
        WriteChild(terminal, "Publish", "terminalhost", TerminalHostPath(root, rid));
        WriteChild(package, "Build", "package", null,
            new XElement("Error", new XAttribute("Condition", "'$(SkipDashboardPublish)' != 'true'"), new XAttribute("Text", "Packaging must not publish.")),
            Write(Path.Combine(root, "package-skip-publish"), "$(SkipDashboardPublish)"),
            Copy(DashboardPath(root, rid), Path.Combine(root, "package-dashboard")));
        WriteChild(dcp, "Build", "dcp-package", null);
        WriteChild(apphost, "Restore", "restore", null);
        WriteChild(tray, "Publish", "tray", Path.Combine(trayApp, "Contents", "MacOS", "aspire-tray"),
            Write(Path.Combine(trayApp, "Contents", "Info.plist"), "plist"),
            Write(Path.Combine(trayApp, "Contents", "Resources", "Aspire.icns"), "icon"));

        var projectPath = Path.Combine(root, "bundle.proj");
        new XDocument(new XElement("Project",
            new XElement("Import", new XAttribute("Project", Path.Combine(RepoRoot.Path, "eng", "Bundle.proj"))),
            new XElement("PropertyGroup",
                Property("ArtifactsDir", Path.Combine(root, "artifacts") + Path.DirectorySeparatorChar),
                Property("ArtifactsLogDir", Path.Combine(root, "artifacts", "log", "Release") + Path.DirectorySeparatorChar),
                Property("DashboardPublishLogPath", Path.Combine(root, "artifacts", "log", "Release", "PublishNativeDashboard.log")),
                Property("ManagedProjectPath", managed),
                Property("DashboardProjectPath", dashboard),
                Property("TerminalHostProjectPath", terminal),
                Property("DashboardPackProjectPath", package),
                Property("DcpPackProjectPath", dcp),
                Property("TrayProjectPath", tray),
                Property("TrayAppPath", trayApp),
                Property("WindowsTrayPath", Path.Combine(root, "artifacts", "tray")),
                Property("AppHostProjectPath", apphost)),
            // Execute the production phase graph and child processes with tiny publish/pack
            // projects. Layout copying substitutes for native execution, not for phase ordering.
            new XElement("Target", new XAttribute("Name", "_RunCreateLayout"),
                Copy(DashboardPath(root, rid), Path.Combine(root, "layout-dashboard")),
                rid.StartsWith("win-", StringComparison.Ordinal)
                    ? Copy(Path.Combine(root, "artifacts", "tray", "aspire-tray.exe"), Path.Combine(root, "layout-tray")) : null,
                Record("layout")),
            new XElement("Target", new XAttribute("Name", "_PublishNativeCli"), new XAttribute("Condition", "'$(SkipNativeBuild)' != 'true'"), Record("cli")),
            new XElement("Target", new XAttribute("Name", "_VerifyNativeTrayArchive"),
                new XAttribute("AfterTargets", "_RunCreateLayout"),
                new XAttribute("Condition", "$(TargetRid.StartsWith('osx-'))"), Record("verify")),
            new XElement("Target", new XAttribute("Name", "_VerifyWindowsTrayArchive"),
                new XAttribute("AfterTargets", "_RunCreateLayout"),
                new XAttribute("Condition", "$(TargetRid.StartsWith('win-'))"), Record("verify")),
            new XElement("Target", new XAttribute("Name", "_CleanCaches")))).Save(projectPath);
        return projectPath;

        XElement Record(string component) => new("WriteLinesToFile",
            new XAttribute("File", events), new XAttribute("Lines", component), new XAttribute("Overwrite", "false"));

        void WriteChild(string path, string target, string component, string? payloadPath, params XElement[] tasks)
        {
            var directories = tasks.Where(task => task.Name == "WriteLinesToFile")
                .Select(task => Path.GetDirectoryName(task.Attribute("File")!.Value)!);
            if (payloadPath is not null)
            {
                directories = directories.Append(Path.GetDirectoryName(payloadPath)!);
            }
            foreach (var directory in directories)
            {
                Directory.CreateDirectory(directory);
            }
            new XDocument(new XElement("Project",
                new XAttribute("DefaultTargets", target),
                target == "Restore" ? null : new XElement("Target", new XAttribute("Name", "Restore")),
                new XElement("Target", new XAttribute("Name", target),
                    Record(component),
                    new XElement("WriteLinesToFile", new XAttribute("File", properties),
                        new XAttribute("Lines", $"{component}|{propertyRecord}"), new XAttribute("Overwrite", "false")),
                    payloadPath is null ? null : Write(payloadPath, component),
                    tasks))).Save(path);
        }
    }

    private static XElement Property(string name, string value) => new(name, value);

    private static XElement Write(string path, string contents) => new("WriteLinesToFile",
        new XAttribute("File", path), new XAttribute("Lines", contents), new XAttribute("Overwrite", "true"));

    private static XElement Copy(string source, string destination) => new("Copy",
        new XAttribute("SourceFiles", source), new XAttribute("DestinationFiles", destination));

    private static string ManagedPath(string root, string rid) =>
        Path.Combine(root, "artifacts", "bin", "Aspire.Managed", "Release", "net10.0", rid, "publish",
            rid.StartsWith("win-", StringComparison.Ordinal) ? "aspire-managed.exe" : "aspire-managed");

    private static string DashboardPath(string root, string rid) =>
        Path.Combine(root, "artifacts", "bin", "Aspire.Dashboard", rid, "Release", "net11.0", rid, "publish",
            rid.StartsWith("win-", StringComparison.Ordinal) ? "Aspire.Dashboard.exe" : "Aspire.Dashboard");

    private static string TerminalHostPath(string root, string rid) =>
        Path.Combine(root, "artifacts", "bin", "Aspire.TerminalHost", "Release", "net11.0", rid, "publish",
            rid.StartsWith("win-", StringComparison.Ordinal) ? "Aspire.TerminalHost.exe" : "Aspire.TerminalHost");

    private async Task RunAsync(string project, string target, string rid, params string[] properties)
    {
        var result = await ExecuteAsync(project, target, rid, properties);
        Assert.True(result.ExitCode == 0, result.Output);
    }

    private async Task<(int ExitCode, string Output)> ExecuteAsync(string project, string target, string rid, params string[] properties)
    {
        var dotnet = Path.Combine(RepoRoot.Path, ".dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var start = new ProcessStartInfo(dotnet)
        {
            WorkingDirectory = Path.GetDirectoryName(project)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        // Bundle's Exec children must use the same SDK as this MSBuild invocation.
        start.Environment["PATH"] = Path.GetDirectoryName(dotnet) + Path.PathSeparator + start.Environment["PATH"];
        foreach (var argument in new[] { "msbuild", project, "-nologo", $"-t:{target}", "-p:Configuration=Release",
            "-p:ContinuousIntegrationBuild=true", $"-p:TargetRid={rid}", "-p:VersionPrefix=42.0.0",
            "-p:VersionSuffix=ci.test", "-p:OfficialBuildId=20261014.1", "-p:AspireCliChannel=staging",
            "-p:DotNetFinalVersionKind=release", "-p:StabilizePackageVersion=true", "-p:IsOfficialBuild=true" }
            .Concat(properties.Select(property => "-p:" + property)))
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
        var text = await stdout + await stderr;
        output.WriteLine(text);
        return (process.ExitCode, text);
    }
}
