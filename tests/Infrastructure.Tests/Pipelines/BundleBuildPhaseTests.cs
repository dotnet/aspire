// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Xml.Linq;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

public sealed class BundleBuildPhaseTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("Build")]
    [InlineData("PublishBundleComponents")]
    [InlineData("PackageBundlePayload")]
    public async Task BundleEntryPointsValidateBeforeCreatingLogsOrRunningChildren(string target)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, "linux-x64");
        foreach (var (property, message) in new[]
        {
            ("Pack=true", "Pack is no longer a Bundle.proj input."),
            ("Pack=false", "Pack is no longer a Bundle.proj input."),
            ("Sign=invalid", "Sign must be true or false."),
            ("Sign=true", "Signing requires BuildDistributionPackages=true."),
            ("DotNetSignType=invalid", "DotNetSignType must be empty, test, or real."),
            ("DotNetSignType=real", "Real signing requires Sign=true."),
            ("VersionSuffix=unsafe&argument", "VersionSuffix must contain"),
            ("AspireCliChannel=unsafe&argument", "AspireCliChannel must contain"),
            ("PackDcpPackage=invalid", "PackDcpPackage must be true or false."),
            ("ContinuousIntegrationBuild=invalid", "ContinuousIntegrationBuild must be true or false.")
        })
        {
            var result = await ExecuteAsync(project, target, "linux-x64", property);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains(message, result.Output);
            Assert.False(Directory.Exists(Path.Combine(workspace.Path, "artifacts", "log")));
            Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
        }
    }

    [Fact]
    public async Task DashboardPublishingHonorsPublicLogPathOverride()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        var logPath = Path.Combine(workspace.Path, "custom logs", "dashboard.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        await RunAsync(project, "PublishBundleComponents", rid, "BuildDistributionPackages=true",
            "DashboardPublishLogPath=" + logPath);

        Assert.True(File.Exists(logPath));
        Assert.Contains("shared Dashboard publish warning", await File.ReadAllTextAsync(logPath));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task MacOSSigningRemovesDebugSymbolsFromPreparedComponents(bool terminalSymbols, bool trailingSeparator)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateOrchestrationProject(workspace.Path, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        var dashboard = Path.GetDirectoryName(DashboardPath(workspace.Path, rid))!;
        var terminal = Path.GetDirectoryName(TerminalHostPath(workspace.Path, rid))!;
        var server = Path.GetDirectoryName(AppHostServerPath(workspace.Path, rid))!;
        Directory.CreateDirectory(Path.Combine(server, "Aspire.Hosting.Native.Server.dSYM", "Contents"));
        if (terminalSymbols)
        {
            Directory.CreateDirectory(Path.Combine(terminal, "Aspire.TerminalHost.dSYM", "Contents"));
        }
        var separator = trailingSeparator ? Path.DirectorySeparatorChar.ToString() : "";
        await RunAsync(project, "Build", rid, "BuildDistributionPackages=true", "Sign=true", "DotNetSignType=test",
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipTrayPublish=true", "SkipNativeCliWithEmbeddedBundle=true",
            "_DashboardPublishDir=" + dashboard + separator,
            "_TerminalHostPublishDir=" + terminal + separator,
            "_AppHostServerPublishDir=" + server + Path.DirectorySeparatorChar);

        Assert.False(Directory.Exists(Path.Combine(dashboard, "Aspire.Dashboard.dSYM")));
        Assert.False(Directory.Exists(Path.Combine(terminal, "Aspire.TerminalHost.dSYM")));
        Assert.False(Directory.Exists(Path.Combine(server, "Aspire.Hosting.Native.Server.dSYM")));
        Assert.Equal("managed", File.ReadAllText(ManagedPath(workspace.Path, rid)).Trim());
        Assert.Equal("terminalhost", File.ReadAllText(TerminalHostPath(workspace.Path, rid)).Trim());
        Assert.Equal("signed Dashboard", File.ReadAllText(DashboardPath(workspace.Path, rid)).Trim());
        Assert.Equal("signed AppHost Server", File.ReadAllText(AppHostServerPath(workspace.Path, rid)).Trim());
    }

    [Theory]
    [InlineData("SkipAspireManagedPublish", "managed")]
    [InlineData("SkipDashboardPublish", "dashboard")]
    [InlineData("SkipTerminalHostPublish", "terminalhost")]
    [InlineData("SkipAppHostServerPublish", "apphost-server")]
    [InlineData("SkipTrayPublish", "tray")]
    public async Task ComponentPublishControlsReuseOnlyTheirOwnPreparedBytes(string control, string component)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        var paths = new Dictionary<string, string>
        {
            ["managed"] = ManagedPath(workspace.Path, rid),
            ["dashboard"] = DashboardPath(workspace.Path, rid),
            ["terminalhost"] = TerminalHostPath(workspace.Path, rid),
            ["apphost-server"] = AppHostServerPath(workspace.Path, rid),
            ["tray"] = Path.Combine(workspace.Path, "artifacts", "tray", "Aspire Tray.app", "Contents", "MacOS", "aspire-tray")
        };
        File.WriteAllText(paths[component], "prepared bytes");
        File.Delete(Path.Combine(workspace.Path, "events"));

        await RunAsync(project, "PublishBundleComponents", rid, control + "=true");

        Assert.Equal(paths.Keys.Where(name => name != component),
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.Equal("prepared bytes", File.ReadAllText(paths[component]));
    }

    [Theory]
    [InlineData("SkipNativeBuild", "SkipNativeCliWithEmbeddedBundle")]
    [InlineData("SkipManagedBuild", "SkipAspireManagedPublish")]
    [InlineData("SkipAppHostServerBuild", "SkipAppHostServerPublish")]
    [InlineData("SkipTrayBuild", "SkipTrayPublish")]
    public async Task RetiredBundleControlsFailBeforeAnyWork(string retired, string replacement)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, "linux-x64");

        var result = await ExecuteAsync(project, "Build", "linux-x64", retired + "=false");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(retired + " is no longer a Bundle.proj input.", result.Output);
        Assert.Contains(replacement, result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("SkipAspireManagedPublish")]
    [InlineData("SkipDashboardPublish")]
    [InlineData("SkipTerminalHostPublish")]
    [InlineData("SkipAppHostServerPublish")]
    [InlineData("SkipTrayPublish")]
    [InlineData("SkipNativeCliWithEmbeddedBundle")]
    public async Task InvalidPublishControlsFailBeforeAnyWork(string control)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, "linux-x64");

        var result = await ExecuteAsync(project, "Build", "linux-x64", control + "=invalid");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(control + " must be true or false.", result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Fact]
    public async Task DcpRestoreFailureStopsBeforePublishingAnyComponents()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, "linux-x64");
        File.WriteAllText(Path.Combine(workspace.Path, "fail-restore"), "");

        var result = await ExecuteAsync(project, "Build", "linux-x64", "SkipNativeCliWithEmbeddedBundle=true");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Failed publishing restore", result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("BuildDistributionPackages=invalid", "BuildDistributionPackages must be true or false.")]
    [InlineData("SkipNativeCliWithEmbeddedBundle=invalid", "SkipNativeCliWithEmbeddedBundle must be true or false.")]
    [InlineData("BuildDistributionPackages=true;Pack=false", "Pack is no longer a Bundle.proj input.")]
    public async Task BundleRejectsInvalidOrConflictingBuildSwitches(string settings, string message)
    {
        var result = await ExecuteAsync(Path.Combine(RepoRoot.Path, "eng", "Bundle.proj"),
            "_ShowBanner", "linux-x64", settings.Split(';'));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(message, result.Output);
    }

    [Theory]
    [InlineData("linux-x64", "", false)]
    [InlineData("win-x64", "test", true)]
    [InlineData("win-x64", "test", false)]
    [InlineData("osx-arm64", "", false)]
    [InlineData("osx-arm64", "test", true)]
    [InlineData("osx-arm64", "real", true)]
    public async Task NativeOrchestrationPackagesFinalSignedBytesOnce(string rid, string signType, bool sign)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.CreateDirectory("native orchestration with spaces").FullName;
        var project = CreateOrchestrationProject(root, rid);
        // Windows publish requires a Windows AOT lane; provide the same prepared
        // outputs on other hosts while still executing the production sign/pack graph.
        var windows = rid.StartsWith("win-", StringComparison.Ordinal);
        if (windows)
        {
            await RunAsync(project, "PublishBundleComponents", rid, "SkipTrayPublish=true");
            var tray = Path.Combine(root, "artifacts", "tray");
            Directory.CreateDirectory(tray);
            File.WriteAllText(Path.Combine(tray, "aspire-tray.exe"), "tray");
            File.WriteAllText(Path.Combine(tray, "Aspire.ico"), "icon");
            File.Delete(Path.Combine(root, "events"));
            File.Delete(Path.Combine(root, "cache-properties"));
        }

        await RunAsync(project, "Build", rid, "BuildDistributionPackages=true",
            $"Sign={sign}", $"DotNetSignType={signType}",
            $"SkipAspireManagedPublish={windows}", $"SkipDashboardPublish={windows}", $"SkipTerminalHostPublish={windows}",
            $"SkipAppHostServerPublish={windows}", $"SkipTrayPublish={windows}", "SkipNativeCliWithEmbeddedBundle=false",
            "BundleVersion=ci-bundlepayload", "RestorePackagesPath=" + Path.Combine(root, "restore cache"), "PackDcpPackage=true");

        var expected = new List<string> { "restore" };
        if (!windows)
        {
            expected.AddRange(["managed", "dashboard", "terminalhost", "apphost-server"]);
        }
        if (rid.StartsWith("osx-", StringComparison.Ordinal))
        {
            Assert.Equal(signType == "real" ? "true" : "false",
                File.ReadAllText(Path.Combine(root, "require-tray-signature")).Trim());
            expected.AddRange(["tray", "adhoc"]);
            if (signType == "real")
            {
                expected.Add("prepare-tray");
            }
        }
        if (sign)
        {
            expected.Add("sign-components");
        }
        if (signType == "real")
        {
            expected.AddRange(["sign-tray", "notarize-tray", "restore-tray"]);
        }
        expected.AddRange(["package", "dcp-package", "layout"]);
        if (!rid.StartsWith("linux-", StringComparison.Ordinal))
        {
            expected.Add("verify");
        }
        expected.Add("cli");
        if (sign)
        {
            expected.Add("sign-archive");
        }
        expected.Add("cli-pack");
        Assert.Equal(expected, File.ReadAllLines(Path.Combine(root, "events")));
        var dashboardBytes = sign ? "signed Dashboard" : "dashboard";
        Assert.Equal(dashboardBytes, File.ReadAllText(Path.Combine(root, "package-dashboard")).Trim());
        Assert.Equal(dashboardBytes, File.ReadAllText(Path.Combine(root, "layout-dashboard")).Trim());
        Assert.Equal(dashboardBytes, File.ReadAllText(Path.Combine(root, "cli-payload")).Trim());
        Assert.Equal(sign ? "signed AppHost Server" : "apphost-server",
            File.ReadAllText(Path.Combine(root, "layout-apphost-server")).Trim());
        Assert.Equal(sign ? "signed CLI archive" : "CLI archive", File.ReadAllText(Path.Combine(root, "cli-package")).Trim());
        if (rid.StartsWith("osx-", StringComparison.Ordinal))
        {
            Assert.Equal(signType == "real" ? "signed tray" : "tray",
                File.ReadAllText(Path.Combine(root, "layout-tray")).Trim());
            if (sign && !OperatingSystem.IsWindows())
            {
                foreach (var path in new[] { ManagedPath(root, rid), DashboardPath(root, rid), TerminalHostPath(root, rid), AppHostServerPath(root, rid) })
                {
                    Assert.True(File.GetUnixFileMode(path).HasFlag(UnixFileMode.UserExecute));
                }
            }
        }
        if (sign)
        {
            Assert.Equal("False", File.ReadAllText(Path.Combine(root, "component-archive-exists")).Trim());
        }
        else
        {
            Assert.False(File.Exists(Path.Combine(root, "component-archive-exists")));
        }
        var cliProperties = File.ReadAllText(Path.Combine(root, "cli-properties")).Trim().Split('|');
        Assert.Equal(["Release", rid, "ci.test", "20261014.1", "staging", ""], cliProperties[..6]);
        var expectedCache = Path.GetFullPath(Path.Combine(root, "restore cache"));
        Assert.Equal(expectedCache, Path.TrimEndingDirectorySeparator(Path.GetFullPath(cliProperties[6])));
        foreach (var line in File.ReadAllLines(Path.Combine(root, "cache-properties")))
        {
            var values = line.Split('|');
            // MSBuild tasks forward cache properties, not an Exec environment.
            var caches = values[0] is "prepare-tray" or "restore-tray" ? values[2..] : values[1..];
            Assert.All(caches, cache =>
                Assert.Equal(expectedCache, Path.TrimEndingDirectorySeparator(Path.GetFullPath(cache))));
        }
    }

    [Theory]
    [InlineData("warning", "dashboard")]
    [InlineData("adhoc", "tray")]
    [InlineData("sign-components", "prepare-tray")]
    [InlineData("sign-tray", "sign-components")]
    [InlineData("notarize-tray", "sign-tray")]
    [InlineData("restore-tray", "notarize-tray")]
    [InlineData("package", "restore-tray")]
    [InlineData("dcp-package", "package")]
    [InlineData("restore", null)]
    [InlineData("layout", "dcp-package")]
    [InlineData("verify", "layout")]
    [InlineData("cli", "verify")]
    [InlineData("sign-archive", "cli")]
    [InlineData("cli-pack", "sign-archive")]
    public async Task NativeOrchestrationStopsOnFailedPhase(string phase, string? lastEvent)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateOrchestrationProject(workspace.Path, rid);
        File.WriteAllText(Path.Combine(workspace.Path, "fail-" + phase), "");

        var result = await ExecuteAsync(project, "Build", rid, "BuildDistributionPackages=true",
            "Sign=true", "DotNetSignType=real", "PackDcpPackage=true", "BundleVersion=ci-bundlepayload");

        Assert.NotEqual(0, result.ExitCode);
        if (lastEvent is null)
        {
            Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
        }
        else
        {
            Assert.Equal(lastEvent, File.ReadAllLines(Path.Combine(workspace.Path, "events"))[^1]);
        }
    }

    [Theory]
    [InlineData("Pack=invalid", "Pack is no longer a Bundle.proj input.")]
    [InlineData("BuildDistributionPackages=false", "Signing requires BuildDistributionPackages=true")]
    [InlineData("Sign=invalid", "Sign must be true or false.")]
    [InlineData("Sign=true", "Signing requires DotNetSignType")]
    [InlineData("DotNetSignType=invalid", "DotNetSignType must be")]
    [InlineData("DotNetSignType=real", "Real signing requires")]
    [InlineData("VersionSuffix=unsafe&argument", "VersionSuffix must contain")]
    [InlineData("AspireCliChannel=unsafe&argument", "AspireCliChannel must contain")]
    public async Task NativeOrchestrationRejectsInconsistentInputsBeforePublishing(string property, string message)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateOrchestrationProject(workspace.Path, "osx-arm64");

        var result = await ExecuteAsync(project, "Build", "osx-arm64", "BuildDistributionPackages=true",
            property == "BuildDistributionPackages=false" ? "Sign=true" : "Sign=false", property);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(message, result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Fact]
    public async Task LinuxOfficialIdentityDoesNotEnableSigning()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateOrchestrationProject(workspace.Path, "linux-x64");

        var result = await ExecuteAsync(project, "Build", "linux-x64", "BuildDistributionPackages=true", "Sign=true", "DotNetSignType=test");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Linux native bundle lanes must disable signing.", result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("OfficialBuild=false", "Real signing requires an official build identity")]
    [InlineData("ForceDryRunSigning=true", "Real signing requires an official build identity")]
    public async Task RealSigningRejectsDryRunIdentity(string property, string message)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateOrchestrationProject(workspace.Path, "osx-arm64");

        var result = await ExecuteAsync(project, "Build", "osx-arm64", "BuildDistributionPackages=true",
            "Sign=true", "DotNetSignType=real", property);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(message, result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Fact]
    public async Task MissingSigningPluginFailsInSigningChildBeforePackaging()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateOrchestrationProject(workspace.Path, "osx-arm64");
        File.Delete(Path.Combine(workspace.Path, "signing-plugin.props"));

        var result = await ExecuteAsync(project, "Build", "osx-arm64", "BuildDistributionPackages=true",
            "Sign=true", "DotNetSignType=real");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Signing child requires the MicroBuild plugin.", result.Output);
        Assert.Equal(["restore", "managed", "dashboard", "terminalhost", "apphost-server", "tray", "adhoc", "prepare-tray"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TraySigningValidatesPluginAfterRealPackageImports(bool installed)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The production tray signing validator requires macOS.");
        using var workspace = TemporaryWorkspace.Create(output);
        var plugins = workspace.CreateDirectory("plugins").FullName;
        if (installed)
        {
            // Use MicroBuild Core's documented override discovery, not a global
            // SigningEnabled property that would bypass the package import.
            var build = workspace.CreateDirectory("plugins/MicroBuild.Plugins.Signing/1.0.0/build").FullName;
            new XDocument(new XElement("Project",
                new XElement("PropertyGroup", Property("MicroBuild_SigningEnabled", "true"))))
                .Save(Path.Combine(build, "MicroBuild.Plugins.Signing.props"));
        }
        var archive = Path.Combine(workspace.Path, "AspireTray.zip");
        File.WriteAllText(archive, "prepared tray");
        var project = Path.Combine(RepoRoot.Path, "eng", "pipelines", "SignTray.proj");
        var environment = new Dictionary<string, string?>
        {
            ["MicroBuild_SigningEnabled"] = null,
            ["MicroBuildOverridePluginDirectory"] = plugins,
            ["NUGET_PACKAGES"] = Path.Combine(workspace.Path, "cache")
        };
        string[] properties =
        [
            "RuntimeIdentifier=osx-arm64", "SignType=real", "TraySigningArchive=" + archive,
            "MSBuildProjectExtensionsPath=" + Path.Combine(workspace.Path, "obj") + Path.DirectorySeparatorChar,
            "BaseIntermediateOutputPath=" + Path.Combine(workspace.Path, "obj") + Path.DirectorySeparatorChar,
            "ArtifactsDir=" + Path.Combine(workspace.Path, "artifacts") + Path.DirectorySeparatorChar
        ];
        var restore = await ExecuteAsync(project, "Restore", "osx-arm64", environment, properties);
        Assert.True(restore.ExitCode == 0, restore.Output);

        // Validate only: this must never submit a signing or notarization request.
        var result = await ExecuteAsync(project, "ValidateTraySigning", "osx-arm64", environment, properties);

        Assert.Equal(installed, result.ExitCode == 0);
        if (!installed)
        {
            Assert.Contains("The official MicroBuild signing plugin must be enabled for tray signing.", result.Output);
        }
    }

    [Theory]
    [InlineData("linux-x64", false)]
    [InlineData("linux-arm64", true)]
    [InlineData("osx-arm64", false)]
    public async Task PackagingPreservesPreparedBytesWithoutPublishing(string rid, bool packDcp)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.CreateDirectory("bundle inputs with spaces").FullName;
        var project = CreateProject(root, rid);

        await RunAsync(project, "PublishBundleComponents", rid, "SkipNativeCliWithEmbeddedBundle=true", "SuppressAOT=true");
        var dashboard = DashboardPath(root, rid);
        File.WriteAllText(dashboard, "signed Dashboard");
        await RunAsync(project, "PackageBundlePayload", rid, "BuildDistributionPackages=true", $"PackDcpPackage={packDcp}", "SkipNativeCliWithEmbeddedBundle=true", "SuppressAOT=true");

        Assert.Equal("signed Dashboard", File.ReadAllText(dashboard));
        Assert.Equal("signed Dashboard", File.ReadAllText(Path.Combine(root, "package-dashboard")));
        Assert.Equal("signed Dashboard", File.ReadAllText(Path.Combine(root, "layout-dashboard")));
        var expected = new List<string> { "managed", "dashboard", "terminalhost", "apphost-server" };
        if (rid.StartsWith("osx-", StringComparison.Ordinal))
        {
            expected.Add("tray");
        }
        expected.AddRange(["restore", "package"]);
        if (packDcp)
        {
            expected.Add("dcp-package");
        }
        expected.Add("layout");
        if (rid.StartsWith("osx-", StringComparison.Ordinal))
        {
            expected.Add("verify");
        }
        Assert.Equal(expected,
            File.ReadAllLines(Path.Combine(root, "events")).Select(line => line.Split('|')[0]));

        foreach (var line in File.ReadAllLines(Path.Combine(root, "properties")))
        {
            var values = line.Split('|');
            var runtimeIdentifier = values[0] is "managed" or "dashboard" or "terminalhost" or "apphost-server" or "tray" ? rid : "";
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
            var result = await ExecuteAsync(project, target, rid, "BuildDistributionPackages=true");
            Assert.NotEqual(0, result.ExitCode);
        }
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("managed")]
    [InlineData("dashboard")]
    [InlineData("terminalhost")]
    [InlineData("apphost-server")]
    [InlineData("ats-contract")]
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
            "apphost-server" => AppHostServerPath(workspace.Path, rid),
            "ats-contract" => Path.Combine(Path.GetDirectoryName(AppHostServerPath(workspace.Path, rid))!, "ats-contract.json"),
            "script" => Path.Combine(Path.GetDirectoryName(DashboardPath(workspace.Path, rid))!, "wwwroot", "_framework", "blazor.web.js"),
            _ => Path.Combine(Path.GetDirectoryName(DashboardPath(workspace.Path, rid))!, "libe_sqlite3.so")
        };
        File.Delete(file);
        var before = File.ReadAllLines(Path.Combine(workspace.Path, "events"));

        var result = await ExecuteAsync(project, "PackageBundlePayload", rid, "BuildDistributionPackages=true");

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
            ManagedPath(root, rid), DashboardPath(root, rid), TerminalHostPath(root, rid), AppHostServerPath(root, rid),
            Path.Combine(Path.GetDirectoryName(AppHostServerPath(root, rid))!, "ats-contract.json"),
            Path.Combine(dashboardDirectory, "wwwroot", "_framework", "blazor.web.js"),
            Path.Combine(dashboardDirectory, "e_sqlite3.dll"), tray,
            Path.Combine(root, "artifacts", "tray", "Aspire.ico")
        })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "signed prepared bytes");
        }

        await RunAsync(project, "PackageBundlePayload", rid, "BuildDistributionPackages=true", "SkipNativeCliWithEmbeddedBundle=true");

        Assert.Equal("false", File.ReadAllText(Path.Combine(root, "require-tray-signature")).Trim());
        Assert.Equal(["restore", "package", "layout", "verify"],
            File.ReadAllLines(Path.Combine(root, "events")));
        Assert.Equal("signed prepared bytes", File.ReadAllText(Path.Combine(root, "package-dashboard")));
        Assert.Equal("signed prepared bytes", File.ReadAllText(Path.Combine(root, "layout-dashboard")));
        Assert.Equal("signed prepared bytes", File.ReadAllText(tray));
        Assert.Equal("signed prepared bytes", File.ReadAllText(Path.Combine(root, "layout-tray")));

        File.Delete(tray);
        var before = File.ReadAllLines(Path.Combine(root, "events"));
        var result = await ExecuteAsync(project, "PackageBundlePayload", rid, "BuildDistributionPackages=true");
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
        await RunAsync(project, "Build", rid, "SkipNativeCliWithEmbeddedBundle=true");
        Assert.Equal(["restore", "managed", "dashboard", "terminalhost", "apphost-server", "layout"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")).Select(line => line.Split('|')[0]));

        File.Delete(Path.Combine(workspace.Path, "events"));
        await RunAsync(project, "Build", rid, "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipNativeCliWithEmbeddedBundle=true", "SkipTrayPublish=true");
        Assert.Equal(["restore", "layout"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")).Select(line => line.Split('|')[0]));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BuildSelectsExactlyOneCliBackendUnlessEmbeddedCliIsSkipped(bool pack, bool skipNative)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        var bundle = XDocument.Load(project);
        bundle.Root!.Add(new XElement("Target", new XAttribute("Name", "_BuildCliDistributionPackages"),
            new XAttribute("Condition", "'$(BuildDistributionPackages)' == 'true' and '$(SkipNativeCliWithEmbeddedBundle)' != 'true'"),
            Write(Path.Combine(workspace.Path, "distribution-cli"), "distribution CLI")));
        bundle.Save(project);

        // Omit both target and the packaging switch in the default case. CI identity and a
        // distribution-only modifier must not implicitly enable packaging.
        string[] properties = pack
            ? ["BuildDistributionPackages=true", $"SkipNativeCliWithEmbeddedBundle={skipNative}", "PackDcpPackage=true"]
            : [$"SkipNativeCliWithEmbeddedBundle={skipNative}", "PackDcpPackage=true"];
        await RunAsync(project, pack ? "Build" : "", rid, properties);

        var expected = new List<string> { "restore", "managed", "dashboard", "terminalhost", "apphost-server" };
        if (pack)
        {
            expected.AddRange(["package", "dcp-package"]);
        }
        expected.Add("layout");
        if (!pack && !skipNative)
        {
            expected.Add("cli");
        }
        Assert.Equal(expected, File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.Equal(pack && !skipNative, File.Exists(Path.Combine(workspace.Path, "distribution-cli")));
        Assert.Equal(pack, File.Exists(Path.Combine(workspace.Path, "package-dashboard")));
        Assert.Equal("dashboard", File.ReadAllText(Path.Combine(workspace.Path, "layout-dashboard")).Trim());
    }

    [Fact]
    public async Task LightweightBuildUsesRealCliPublisherWithDebugCustomOutputAndEmbeddedPayload()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.CreateDirectory("local bundle with spaces").FullName;
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var project = CreateOrchestrationProject(root, rid);
        var cli = Path.Combine(root, "fake-cli.csproj");
        var publish = Path.Combine(root, "custom CLI output");
        // Isolate the tiny SDK publisher from the repository's product imports.
        File.WriteAllText(Path.Combine(root, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(root, "Directory.Build.targets"), "<Project />");
        var assemblyName = "BundleCliFixture" + Guid.NewGuid().ToString("N");
        new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup",
                Property("TargetFramework", "net11.0"), Property("AssemblyName", assemblyName)),
            new XElement("ItemGroup",
                new XElement("EmbeddedResource", new XAttribute("Include", "$(BundlePayloadPath)"),
                    new XAttribute("LogicalName", "bundle.tar.gz"))),
            new XElement("Target", new XAttribute("Name", "RecordPublish"), new XAttribute("AfterTargets", "Publish"),
                Write(Path.Combine(root, "local-cli-properties"),
                    "$(Configuration)|$(ContinuousIntegrationBuild)|$(BundlePayloadPath)|$(PublishDir)|$(SkipNativeBuild)"))))
            .Save(cli);
        var bundle = XDocument.Load(project);
        foreach (var target in bundle.Root!.Elements("Target").Where(target =>
            target.Attribute("Name")!.Value == "_PublishNativeCliWithEmbeddedBundle").ToArray())
        {
            target.Remove();
        }
        bundle.Root.Element("PropertyGroup")!.Add(Property("_CliProjectPath", cli));
        bundle.Save(project);
        File.WriteAllText(Path.Combine(root, "fail-warning"), "");
        File.WriteAllText(Path.Combine(root, "fail-adhoc"), "");

        await RunAsync(project, "", rid, "ContinuousIntegrationBuild=false", "Configuration=Debug",
            "CliPublishDir=" + publish, "BundleVersion=local-debug", "PackDcpPackage=true");

        var archive = Path.Combine(root, "artifacts", "bundle", $"aspire-local-debug-{rid}.tar.gz");
        var properties = File.ReadAllText(Path.Combine(root, "local-cli-properties")).Trim().Split('|');
        Assert.Equal(["Debug", "false"], properties[..2]);
        Assert.Equal(Path.GetFullPath(archive), Path.GetFullPath(properties[2]));
        Assert.Equal(Path.GetFullPath(publish), Path.TrimEndingDirectorySeparator(Path.GetFullPath(properties[3])));
        Assert.Equal("", properties[4]);
        foreach (var name in new[] { "PublishManaged", "PublishDashboard", "PublishTerminalHost", "PublishAppHostServer", "RestoreDcp", "PublishCli" })
        {
            var binlog = Path.Combine(root, "artifacts", "log", "Release", name + ".binlog");
            Assert.True(File.Exists(binlog), $"Missing local build binlog: {binlog}");
            Assert.True(new FileInfo(binlog).Length > 0, $"Empty local build binlog: {binlog}");
        }
        var assembly = System.Reflection.Assembly.Load(File.ReadAllBytes(Path.Combine(publish, assemblyName + ".dll")));
        using var payload = assembly.GetManifestResourceStream("bundle.tar.gz");
        Assert.NotNull(payload);
        using var reader = new StreamReader(payload);
        Assert.Equal(File.ReadAllText(archive), await reader.ReadToEndAsync());
        Assert.Equal(rid.StartsWith("linux-", StringComparison.Ordinal)
                ? ["restore", "managed", "dashboard", "terminalhost", "apphost-server", "layout"]
                : ["restore", "managed", "dashboard", "terminalhost", "apphost-server",
                    rid.StartsWith("win-", StringComparison.Ordinal) ? "windows-tray" : "tray", "layout", "verify"],
            File.ReadAllLines(Path.Combine(root, "events")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DistributionPublishingVerifiesNativeDashboardWarnings(bool unexpectedWarning)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        if (unexpectedWarning)
        {
            File.WriteAllText(Path.Combine(workspace.Path, "fail-warning"), "");
        }

        var result = await ExecuteAsync(project, "PublishBundleComponents", rid, "BuildDistributionPackages=true");

        if (unexpectedWarning)
        {
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("Expected exactly one IL2026 warning", result.Output);
        }
        else
        {
            Assert.True(result.ExitCode == 0, result.Output);
            Assert.Contains("Verified the expected Native AOT Dashboard IL2026 warning", result.Output);
        }
        Assert.Equal(unexpectedWarning ? ["managed", "dashboard"] : ["managed", "dashboard", "terminalhost", "apphost-server"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DistributionDashboardReuseDoesNotRequireAPublishWarningLog(bool skipManaged)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        File.Delete(Path.Combine(workspace.Path, "events"));
        File.Delete(Path.Combine(workspace.Path, "artifacts", "log", "Release", "PublishNativeDashboard.log"));
        File.WriteAllText(Path.Combine(workspace.Path, "fail-warning"), "");

        await RunAsync(project, "Build", rid, "BuildDistributionPackages=true", "SkipNativeCliWithEmbeddedBundle=true",
            $"SkipAspireManagedPublish={skipManaged}", "SkipDashboardPublish=true");

        Assert.Equal(skipManaged
                ? ["restore", "terminalhost", "apphost-server", "package", "layout"]
                : ["restore", "managed", "terminalhost", "apphost-server", "package", "layout"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.False(File.Exists(Path.Combine(workspace.Path, "artifacts", "log", "Release", "PublishNativeDashboard.log")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SkippingCliStillSignsNewPayloadPackagesInAFreshEvaluation(bool packDcp)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateOrchestrationProject(workspace.Path, rid);
        File.WriteAllText(Path.Combine(workspace.Path, "fail-cli"), "");
        File.WriteAllText(Path.Combine(workspace.Path, "fail-cli-pack"), "");

        await RunAsync(project, "Build", rid, "BuildDistributionPackages=true", "Sign=true", "DotNetSignType=real",
            "SkipNativeCliWithEmbeddedBundle=true", $"PackDcpPackage={packDcp}", "BundleVersion=ci-bundlepayload");

        var expected = new List<string>
        {
            "restore", "managed", "dashboard", "terminalhost", "apphost-server", "tray", "adhoc", "prepare-tray",
            "sign-components", "sign-tray", "notarize-tray", "restore-tray", "package"
        };
        if (packDcp)
        {
            expected.Add("dcp-package");
        }
        expected.AddRange(["layout", "verify", "sign-packages"]);
        Assert.Equal(expected, File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.Equal("signed Dashboard package", File.ReadAllText(Path.Combine(workspace.Path, "shipping", "dashboard.nupkg")).Trim());
        Assert.Equal(packDcp, File.Exists(Path.Combine(workspace.Path, "shipping", "dcp.nupkg")));
        if (packDcp)
        {
            Assert.Equal("signed DCP package", File.ReadAllText(Path.Combine(workspace.Path, "shipping", "dcp.nupkg")).Trim());
        }
        Assert.Equal("signed Dashboard", File.ReadAllText(Path.Combine(workspace.Path, "layout-dashboard")).Trim());
        Assert.False(File.Exists(Path.Combine(workspace.Path, "cli-payload")));
        Assert.False(File.Exists(Path.Combine(workspace.Path, "cli-package")));
        Assert.False(File.Exists(Path.Combine(workspace.Path, "aspire-cli-final.tar.gz")));
        foreach (var line in File.ReadAllLines(Path.Combine(workspace.Path, "properties")))
        {
            Assert.Equal("", line.Split('|')[11]);
        }
    }

    [Fact]
    public async Task SkippedCliPayloadPackageSigningFailureFailsTheBuild()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateOrchestrationProject(workspace.Path, rid);
        File.WriteAllText(Path.Combine(workspace.Path, "fail-sign-packages"), "");

        var result = await ExecuteAsync(project, "Build", rid, "BuildDistributionPackages=true", "Sign=true",
            "DotNetSignType=real", "SkipNativeCliWithEmbeddedBundle=true");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Failed sign-packages", result.Output);
        Assert.Equal("verify", File.ReadAllLines(Path.Combine(workspace.Path, "events"))[^1]);
        Assert.Equal("Dashboard package", File.ReadAllText(Path.Combine(workspace.Path, "shipping", "dashboard.nupkg")).Trim());
        Assert.False(File.Exists(Path.Combine(workspace.Path, "cli-package")));
    }

    [Fact]
    public async Task ReusingAllComponentsStillSignsPreparedInputsAndPayloadPackages()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateOrchestrationProject(workspace.Path, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        File.Delete(Path.Combine(workspace.Path, "events"));

        await RunAsync(project, "Build", rid, "BuildDistributionPackages=true", "Sign=true", "DotNetSignType=real",
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipTrayPublish=true", "SkipNativeCliWithEmbeddedBundle=true",
            "PackDcpPackage=true");

        Assert.Equal(["restore", "adhoc", "prepare-tray", "sign-components", "sign-tray", "notarize-tray",
            "restore-tray", "package", "dcp-package", "layout", "verify", "sign-packages"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.Equal("signed Dashboard", File.ReadAllText(Path.Combine(workspace.Path, "layout-dashboard")).Trim());
        Assert.Equal("signed AppHost Server", File.ReadAllText(Path.Combine(workspace.Path, "layout-apphost-server")).Trim());
        Assert.Equal("signed tray", File.ReadAllText(Path.Combine(workspace.Path, "layout-tray")).Trim());
        Assert.Equal("signed Dashboard package", File.ReadAllText(Path.Combine(workspace.Path, "shipping", "dashboard.nupkg")).Trim());
        Assert.Equal("signed DCP package", File.ReadAllText(Path.Combine(workspace.Path, "shipping", "dcp.nupkg")).Trim());
    }

    [Fact]
    public async Task AspireManagedPublishSkipDoesNotSkipDashboardOrTerminalHost()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        File.Delete(Path.Combine(workspace.Path, "events"));
        var managed = ManagedPath(workspace.Path, rid);
        File.WriteAllText(managed, "prepared managed helper");
        await RunAsync(project, "Build", rid, "SkipAspireManagedPublish=true", "SkipNativeCliWithEmbeddedBundle=true");

        Assert.Equal(["restore", "dashboard", "terminalhost", "apphost-server", "layout"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.Equal("prepared managed helper", File.ReadAllText(managed));
    }

    [Fact]
    public async Task NativeCliSkipStillPublishesTrayAndTraySkipReusesIt()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateProject(workspace.Path, rid);
        await RunAsync(project, "Build", rid, "SkipNativeCliWithEmbeddedBundle=true");
        Assert.Equal(["restore", "managed", "dashboard", "terminalhost", "apphost-server", "tray", "layout", "verify"],
            File.ReadAllLines(Path.Combine(workspace.Path, "events")));

        File.Delete(Path.Combine(workspace.Path, "events"));
        await RunAsync(project, "Build", rid, "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipNativeCliWithEmbeddedBundle=true", "SkipTrayPublish=true");
        Assert.Equal(["restore", "layout", "verify"], File.ReadAllLines(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData(false, false, false, false, "managed,dashboard,terminalhost,apphost-server,tray")]
    [InlineData(false, false, false, true, "managed,dashboard,terminalhost,apphost-server")]
    [InlineData(false, false, true, false, "managed,dashboard,terminalhost,tray")]
    [InlineData(false, false, true, true, "managed,dashboard,terminalhost")]
    [InlineData(false, true, false, false, "managed,terminalhost,apphost-server,tray")]
    [InlineData(false, true, false, true, "managed,terminalhost,apphost-server")]
    [InlineData(false, true, true, false, "managed,terminalhost,tray")]
    [InlineData(false, true, true, true, "managed,terminalhost")]
    [InlineData(true, false, false, false, "dashboard,terminalhost,apphost-server,tray")]
    [InlineData(true, false, false, true, "dashboard,terminalhost,apphost-server")]
    [InlineData(true, false, true, false, "dashboard,terminalhost,tray")]
    [InlineData(true, false, true, true, "dashboard,terminalhost")]
    [InlineData(true, true, false, false, "terminalhost,apphost-server,tray")]
    [InlineData(true, true, false, true, "terminalhost,apphost-server")]
    [InlineData(true, true, true, false, "terminalhost,tray")]
    [InlineData(true, true, true, true, "terminalhost")]
    public async Task ComponentPublishSkipOptionsAreIndependent(
        bool skipManaged, bool skipDashboard, bool skipServer, bool skipTray, string expected)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateProject(workspace.Path, rid);
        var dashboard = DashboardPath(workspace.Path, rid);
        File.WriteAllText(dashboard, "prepared Dashboard");

        await RunAsync(project, "PublishBundleComponents", rid,
            $"SkipAspireManagedPublish={skipManaged}", $"SkipDashboardPublish={skipDashboard}",
            $"SkipAppHostServerPublish={skipServer}", $"SkipTrayPublish={skipTray}", "SkipNativeCliWithEmbeddedBundle=true");

        var events = Path.Combine(workspace.Path, "events");
        Assert.Equal(expected.Split(',', StringSplitOptions.RemoveEmptyEntries),
            File.Exists(events) ? File.ReadAllLines(events) : []);
        Assert.Equal(skipDashboard ? "prepared Dashboard" : "dashboard",
            File.ReadAllText(dashboard).Trim());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingPreparedDashboardIsCheckedIndependentlyOfManagedPublishing(bool skipManaged)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "linux-x64";
        var project = CreateProject(workspace.Path, rid);

        var result = await ExecuteAsync(project, "PublishBundleComponents", rid,
            $"SkipAspireManagedPublish={skipManaged}", "SkipDashboardPublish=true");

        Assert.NotEqual(0, result.ExitCode);
        var events = Path.Combine(workspace.Path, "events");
        Assert.Equal(skipManaged ? [] : new[] { "managed" },
            File.Exists(events) ? File.ReadAllLines(events) : []);
        Assert.Contains("Prepared Native AOT Dashboard was not found", result.Output);
    }

    [Theory]
    [InlineData("managed", "restore")]
    [InlineData("dashboard", "restore,managed")]
    [InlineData("terminalhost", "restore,managed,dashboard")]
    [InlineData("apphost-server", "restore,managed,dashboard,terminalhost")]
    [InlineData("tray", "restore,managed,dashboard,terminalhost,apphost-server")]
    public async Task PublishFailureStopsBeforeLaterComponentsSigningAndPackaging(string component, string expected)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        const string rid = "osx-arm64";
        var project = CreateOrchestrationProject(workspace.Path, rid);
        File.WriteAllText(Path.Combine(workspace.Path, "fail-" + component), "");

        var result = await ExecuteAsync(project, "Build", rid, "BuildDistributionPackages=true", "Sign=true", "DotNetSignType=real");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Failed publishing " + component, result.Output);
        var events = Path.Combine(workspace.Path, "events");
        Assert.Equal(expected.Split(',', StringSplitOptions.RemoveEmptyEntries),
            File.Exists(events) ? File.ReadAllLines(events) : []);
    }

    [Theory]
    [InlineData("win-x64")]
    [InlineData("win-arm64")]
    public async Task WindowsTrayPublishRequiresWindowsUnlessSkipped(string rid)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "This exercises rejection on a non-Windows host.");
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, rid);
        File.WriteAllText(DashboardPath(workspace.Path, rid), "prepared Dashboard");

        var result = await ExecuteAsync(project, "PublishBundleComponents", rid,
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true", "SkipAppHostServerPublish=true");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Windows NativeAOT tray publishing requires a Windows build lane.", result.Output);
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
        await RunAsync(project, "PublishBundleComponents", rid,
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipTrayPublish=true");
        Assert.False(File.Exists(Path.Combine(workspace.Path, "events")));
    }

    [Theory]
    [InlineData("win-x64")]
    [InlineData("win-arm64")]
    public async Task WindowsTrayCanPublishWithoutOtherComponents(string rid)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows tray publishing requires a Windows host.");
        using var workspace = TemporaryWorkspace.Create(output);
        var project = CreateProject(workspace.Path, rid);
        File.WriteAllText(DashboardPath(workspace.Path, rid), "prepared Dashboard");

        await RunAsync(project, "PublishBundleComponents", rid,
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipNativeCliWithEmbeddedBundle=true");

        Assert.Equal(["windows-tray"], File.ReadAllLines(Path.Combine(workspace.Path, "events")));
        Assert.Equal("windows-tray",
            File.ReadAllText(Path.Combine(workspace.Path, "artifacts", "tray", "aspire-tray.exe")).Trim());
        Assert.Equal(Path.GetFullPath(Path.Combine(workspace.Path, "artifacts", "tray")),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(
                File.ReadAllText(Path.Combine(workspace.Path, "windows-tray-publish-dir")).Trim())));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ColdCiPublishRestoresAndImportsRealProtobufBuildTargets(bool explicitRestoreCache, bool environmentCache)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.Path;
        const string rid = "linux-x64";
        var project = CreateProject(root, rid);
        var dashboardProject = Path.Combine(root, "ColdProtobufFixture.csproj");
        var generatedDirectory = Path.Combine(root, "obj", "generated");
        // Isolate the SDK fixture from ancestor product imports and package
        // configuration, including when workspaces are under the repository.
        File.WriteAllText(Path.Combine(root, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(root, "Directory.Build.targets"), "<Project />");
        File.WriteAllText(Path.Combine(root, "Directory.Packages.props"), "<Project />");
        File.WriteAllText(Path.Combine(root, "fixture.proto"), """
            syntax = "proto3";
            option csharp_namespace = "ColdBundle";
            message PreparedComponent { string name = 1; }
            """);
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup",
                Property("BaseIntermediateOutputPath", Path.Combine(root, "obj") + Path.DirectorySeparatorChar),
                Property("DirectoryPackagesPropsPath", Path.Combine(RepoRoot.Path, "Directory.Packages.props"))),
            new XElement("Import", new XAttribute("Project", "Sdk.props"), new XAttribute("Sdk", "Microsoft.NET.Sdk")),
            new XElement("PropertyGroup",
                Property("TargetFramework", "net11.0"),
                Property("Protobuf_OutputPath", generatedDirectory)),
            new XElement("ItemGroup",
                new XElement("PackageReference", new XAttribute("Include", "Grpc.Tools"), new XAttribute("PrivateAssets", "all")),
                new XElement("PackageReference", new XAttribute("Include", "Google.Protobuf")),
                new XElement("Protobuf", new XAttribute("Include", Path.Combine(root, "fixture.proto")),
                    new XAttribute("ProtoRoot", root), new XAttribute("GrpcServices", "None"))),
            new XElement("Import", new XAttribute("Project", "Sdk.targets"), new XAttribute("Sdk", "Microsoft.NET.Sdk")),
            // Execute the real restored Grpc.Tools target without making this regression
            // test compile the full Dashboard or invoke its Native AOT toolchain.
            new XElement("Target", new XAttribute("Name", "Publish"), new XAttribute("DependsOnTargets", "Protobuf_Compile"))))
            .Save(dashboardProject);
        File.WriteAllText(Path.Combine(root, "PreparedComponentConsumer.cs"), """
            using ColdBundle;

            internal static class PreparedComponentConsumer
            {
                public static string GetName() => new PreparedComponent { Name = "prepared" }.Name;
            }
            """);
        var bundle = XDocument.Load(project);
        bundle.Root!.Element("PropertyGroup")!.Element("_DashboardProjectPath")!.Value = dashboardProject;
        bundle.Save(project);

        // Let Arcade select its real import root. Hardcoding NuGetPackageRoot
        // would mask a mismatch with RestorePackagesPath, even in a cold cache.
        var environment = new Dictionary<string, string?>
        {
            ["NUGET_PACKAGES"] = environmentCache ? Path.Combine(root, "environment cache") : null
        };
        var properties = new List<string> { "_RepoRoot=" + root + Path.DirectorySeparatorChar };
        if (explicitRestoreCache)
        {
            properties.Add("RestorePackagesPath=" + Path.Combine(root, "explicit restore cache"));
        }
        var result = await ExecuteAsync(project, "PublishBundleComponents", rid, environment, [.. properties]);

        Assert.True(result.ExitCode == 0, result.Output);
        var build = await ExecuteAsync(dashboardProject, "Build", rid, environment, [.. properties]);
        Assert.True(build.ExitCode == 0, build.Output);
    }

    [Theory]
    [RequiresTools(["pwsh", "tar"])]
    [InlineData("win-x64", false, false)]
    [InlineData("win-arm64", false, true)]
    [InlineData("win-x64", true, false)]
    [InlineData("win-arm64", true, true)]
    public async Task RealWindowsLayoutVerifiesArchiveBeforeCli(string rid, bool invalidIcon, bool skipNative)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.Path;
        var project = CreateProject(root, rid);
        await RunAsync(project, "PublishBundleComponents", rid, "SkipTrayPublish=true");
        var tray = Path.Combine(root, "artifacts", "tray");
        WindowsTrayTestPayload.Create(tray, rid);
        if (!invalidIcon)
        {
            File.Copy(Path.Combine(RepoRoot.Path, "src", "Shared", "Aspire.ico"),
                Path.Combine(tray, "Aspire.ico"), overwrite: true);
        }
        File.Delete(Path.Combine(root, "events"));
        var cache = PrepareRealLayout(project, root, rid);

        var result = await ExecuteAsync(project, "Build", rid, "BuildDistributionPackages=false",
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipTrayPublish=true",
            $"SkipNativeCliWithEmbeddedBundle={skipNative}", "BundleVersion=verifier-test", "RestorePackagesPath=" + cache);

        Assert.True(File.Exists(Path.Combine(root, "artifacts", "bundle", $"aspire-verifier-test-{rid}.tar.gz")), result.Output);
        if (invalidIcon)
        {
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("Windows tray must contain the original", result.Output);
        }
        else
        {
            Assert.True(result.ExitCode == 0, result.Output);
            Assert.Contains($"Verified {rid} Windows tray payload", result.Output);
        }
        Assert.Equal(!invalidIcon && !skipNative ? ["restore", "cli"] : ["restore"],
            File.ReadAllLines(Path.Combine(root, "events")));
    }

    [Fact]
    [RequiresTools(["bash", "lipo", "tar"])]
    public async Task RealMacLayoutVerificationRejectsInvalidArchivedExecutableBeforeCli()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "macOS archive verification requires macOS tools.");
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.Path;
        const string rid = "osx-arm64";
        var project = CreateProject(root, rid);
        await RunAsync(project, "PublishBundleComponents", rid, "SkipTrayPublish=true");
        var tray = MacTrayTestPayload.Create(root);
        File.Delete(Path.Combine(root, "events"));
        var cache = PrepareRealLayout(project, root, rid);

        var result = await ExecuteAsync(project, "Build", rid, "BuildDistributionPackages=false",
            "SkipAspireManagedPublish=true", "SkipDashboardPublish=true", "SkipTerminalHostPublish=true",
            "SkipAppHostServerPublish=true", "SkipTrayPublish=true",
            "TrayAppPath=" + tray, "BundleVersion=verifier-test", "RestorePackagesPath=" + cache);

        Assert.True(File.Exists(Path.Combine(root, "artifacts", "bundle", $"aspire-verifier-test-{rid}.tar.gz")), result.Output);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Tray executable does not contain the required arm64 architecture", result.Output);
        Assert.Equal(["restore"], File.ReadAllLines(Path.Combine(root, "events")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealLayoutUsesTheRestoreCacheWhenTheUserDcpCacheIsCold(bool customCache)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.Path;
        const string rid = "linux-x64";
        var project = CreateProject(root, rid);
        await RunAsync(project, "PublishBundleComponents", rid);
        foreach (var file in Aspire.Shared.TerminalHostPayload.GetRequiredFiles(rid))
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(TerminalHostPath(root, rid))!, file), "terminal payload");
        }
        var cache = Path.Combine(root, customCache ? "caller-cache" : ".packages");
        var dcpDirectory = Path.Combine(cache, "microsoft.developercontrolplane.linux-amd64", "1.2.3", "tools");
        Directory.CreateDirectory(dcpDirectory);
        File.WriteAllText(Path.Combine(dcpDirectory, "dcp"), "DCP from the restore cache");
        var bundle = XDocument.Load(project);
        bundle.Root!.Elements("Target").Single(target => target.Attribute("Name")!.Value == "_RunCreateLayout").Remove();
        bundle.Root.Element("PropertyGroup")!.Add(
            Property("_CreateLayoutProjectPath", Path.Combine(RepoRoot.Path, "tools", "CreateLayout", "CreateLayout.csproj")));
        bundle.Save(project);
        var home = workspace.CreateDirectory("cold-home").FullName;
        var environment = new Dictionary<string, string?>
        {
            ["NUGET_PACKAGES"] = customCache ? cache : null,
            ["HOME"] = home,
            ["USERPROFILE"] = home,
            ["DOTNET_CLI_HOME"] = home
        };

        var result = await ExecuteAsync(project, "PackageBundlePayload", rid, environment, "BuildDistributionPackages=true",
            "_RepoRoot=" + root + Path.DirectorySeparatorChar, "BundleVersion=cold-cache");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal("DCP from the restore cache",
            File.ReadAllText(Path.Combine(root, "artifacts", "bundle", rid, "dcp", "dcp")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "bundle", "aspire-cold-cache-linux-x64.tar.gz")));
    }

    private static string PrepareRealLayout(string project, string root, string rid)
    {
        foreach (var file in Aspire.Shared.TerminalHostPayload.GetRequiredFiles(rid))
        {
            var path = Path.Combine(Path.GetDirectoryName(TerminalHostPath(root, rid))!, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "terminal payload");
        }
        var cache = Path.Combine(root, "layout-cache");
        var dcpRid = rid switch
        {
            "win-x64" => "windows-amd64",
            "win-arm64" => "windows-arm64",
            "osx-arm64" => "darwin-arm64",
            _ => throw new ArgumentOutOfRangeException(nameof(rid))
        };
        var dcp = Path.Combine(cache, "microsoft.developercontrolplane." + dcpRid, "1.2.3", "tools");
        Directory.CreateDirectory(dcp);
        File.WriteAllText(Path.Combine(dcp, "dcp"), "DCP payload");
        var bundle = XDocument.Load(project);
        bundle.Root!.Elements("Target").Single(target => target.Attribute("Name")!.Value == "_RunCreateLayout").Remove();
        bundle.Root.Element("PropertyGroup")!.Add(
            Property("_CreateLayoutProjectPath", Path.Combine(RepoRoot.Path, "tools", "CreateLayout", "CreateLayout.csproj")),
            Property("_BundleOutputDir", Path.Combine(root, "artifacts", "bundle", rid)));
        bundle.Save(project);
        return cache;
    }

    private static string CreateProject(string root, string rid)
    {
        var events = Path.Combine(root, "events");
        var properties = Path.Combine(root, "properties");
        var propertyRecord = "$(Configuration)|$(TargetRid)|$(ContinuousIntegrationBuild)|$(VersionPrefix)|$(VersionSuffix)|$(OfficialBuildId)|$(AspireCliChannel)|$(DotNetFinalVersionKind)|$(StabilizePackageVersion)|$(IsOfficialBuild)|$(SkipNativeBuild)|$(SuppressAOT)|$(RuntimeIdentifier)";
        var managed = Path.Combine(root, "managed.proj");
        var dashboard = Path.Combine(root, "dashboard.proj");
        var terminal = Path.Combine(root, "terminal.proj");
        var server = Path.Combine(root, "server.proj");
        var package = Path.Combine(root, "package.proj");
        var dcp = Path.Combine(root, "dcp.proj");
        var apphost = Path.Combine(root, "apphost.proj");
        var tray = Path.Combine(root, "tray.proj");
        var windowsTray = Path.Combine(root, "windows-tray.proj");
        var trayApp = Path.Combine(root, "artifacts", "tray", "Aspire Tray.app");
        var dashboardDirectory = Path.GetDirectoryName(DashboardPath(root, rid))!;

        WriteChild(managed, "Publish", "managed", ManagedPath(root, rid));
        WriteChild(dashboard, "Publish", "dashboard", DashboardPath(root, rid),
            new XElement("Warning", new XAttribute("Text", "shared Dashboard publish warning")),
            new XElement("Warning", new XAttribute("Code", "IL2026"),
                new XAttribute("Text", "Microsoft.AspNetCore.Http.Json.JsonOptions.CreateDefaultTypeResolver()")),
            new XElement("Warning", new XAttribute("Condition", $"Exists('{Path.Combine(root, "fail-warning")}')"),
                new XAttribute("Code", "IL2026"), new XAttribute("Text", "Unexpected trimming warning")),
            new XElement("MakeDir", new XAttribute("Directories", Path.Combine(dashboardDirectory, "Aspire.Dashboard.dSYM"))),
            Write(Path.Combine(root, "dashboard-platform"), "$(PlatformName)"),
            Write(Path.Combine(dashboardDirectory, "wwwroot", "_framework", "blazor.web.js"), "Blazor"),
            Write(Path.Combine(dashboardDirectory, rid.StartsWith("osx-", StringComparison.Ordinal) ? "libe_sqlite3.dylib"
                : rid.StartsWith("win-", StringComparison.Ordinal) ? "e_sqlite3.dll" : "libe_sqlite3.so"), "SQLite"));
        WriteChild(terminal, "Publish", "terminalhost", TerminalHostPath(root, rid));
        WriteChild(server, "Publish", "apphost-server", AppHostServerPath(root, rid),
            Write(Path.Combine(Path.GetDirectoryName(AppHostServerPath(root, rid))!, "ats-contract.json"), "{}"));
        WriteChild(package, "Build", "package", null,
            new XElement("Error", new XAttribute("Condition", "'$(SkipDashboardPublish)' != 'true'"), new XAttribute("Text", "Packaging must not publish.")),
            Write(Path.Combine(root, "package-skip-publish"), "$(SkipDashboardPublish)"),
            Copy(DashboardPath(root, rid), Path.Combine(root, "package-dashboard")));
        WriteChild(dcp, "Build", "dcp-package", null);
        WriteChild(apphost, "Restore", "restore", null);
        WriteChild(tray, "Publish", "tray", Path.Combine(trayApp, "Contents", "MacOS", "aspire-tray"),
            Write(Path.Combine(trayApp, "Contents", "Info.plist"), "plist"),
            Write(Path.Combine(trayApp, "Contents", "Resources", "Aspire.icns"), "icon"));
        WriteChild(windowsTray, "Publish", "windows-tray", Path.Combine(root, "artifacts", "tray", "aspire-tray.exe"),
            Write(Path.Combine(root, "windows-tray-publish-dir"), "$(PublishDir)"),
            Write(Path.Combine(root, "artifacts", "tray", "Aspire.ico"), "icon"));

        var projectPath = Path.Combine(root, "bundle.proj");
        new XDocument(new XElement("Project",
            new XElement("Import", new XAttribute("Project", Path.Combine(RepoRoot.Path, "eng", "Bundle.proj"))),
            new XElement("PropertyGroup",
                Property("_ArtifactsDir", Path.Combine(root, "artifacts") + Path.DirectorySeparatorChar),
                Property("_ArtifactsLogDir", Path.Combine(root, "artifacts", "log", "Release") + Path.DirectorySeparatorChar),
                Property("DashboardPublishLogPath", Path.Combine(root, "artifacts", "log", "Release", "PublishNativeDashboard.log")),
                Property("_ManagedPublishDir", Path.GetDirectoryName(ManagedPath(root, rid))!),
                Property("_DashboardPublishDir", dashboardDirectory),
                Property("_TerminalHostPublishDir", Path.GetDirectoryName(TerminalHostPath(root, rid))!),
                Property("_AppHostServerPublishDir", Path.GetDirectoryName(AppHostServerPath(root, rid))! + Path.DirectorySeparatorChar),
                Property("_ManagedProjectPath", managed),
                Property("_DashboardProjectPath", dashboard),
                Property("_TerminalHostProjectPath", terminal),
                Property("_AppHostServerProjectPath", server),
                Property("_DashboardPackProjectPath", package),
                Property("_DcpPackProjectPath", dcp),
                Property("_TrayProjectPath", tray),
                Property("_TrayWindowsProjectPath", windowsTray),
                Property("TrayAppPath", trayApp),
                Property("WindowsTrayPath", Path.Combine(root, "artifacts", "tray")),
                Property("_AppHostProjectPath", apphost)),
            // Test-only aliases exercise the production phases in isolation.
            new XElement("Target", new XAttribute("Name", "PublishBundleComponents"),
                new XAttribute("DependsOnTargets", "_PublishBundleComponents")),
            new XElement("Target", new XAttribute("Name", "PackageBundlePayload"),
                new XAttribute("DependsOnTargets", "_PackageBundlePayload")),
            // Native layout and verification are substituted together here.
            // Separate real-layout tests exercise the production target and verifiers.
            new XElement("Target", new XAttribute("Name", "_RunCreateLayout"),
                new XElement("Error", new XAttribute("Condition", $"Exists('{Path.Combine(root, "fail-layout")}')"),
                    new XAttribute("Text", "Failed layout")),
                Copy(DashboardPath(root, rid), Path.Combine(root, "layout-dashboard")),
                Copy(AppHostServerPath(root, rid), Path.Combine(root, "layout-apphost-server")),
                rid.StartsWith("win-", StringComparison.Ordinal)
                    ? Copy(Path.Combine(root, "artifacts", "tray", "aspire-tray.exe"), Path.Combine(root, "layout-tray"))
                    : rid.StartsWith("osx-", StringComparison.Ordinal)
                        ? Copy(Path.Combine(trayApp, "Contents", "MacOS", "aspire-tray"), Path.Combine(root, "layout-tray")) : null,
                Copy(Path.Combine(root, "layout-dashboard"),
                    Path.Combine(root, "artifacts", "bundle", "aspire-$(BundleVersion)-$(TargetRid).tar.gz")),
                Record("layout"),
                rid.StartsWith("linux-", StringComparison.Ordinal) ? null : new XElement("Error",
                    new XAttribute("Condition", $"Exists('{Path.Combine(root, "fail-verify")}')"),
                    new XAttribute("Text", "Failed verify")),
                rid.StartsWith("linux-", StringComparison.Ordinal) ? null : Record("verify"),
                rid.StartsWith("linux-", StringComparison.Ordinal) ? null : Write(Path.Combine(root, "require-tray-signature"),
                    rid.StartsWith("osx-", StringComparison.Ordinal) ? "$(_RequireMacTraySignature)" : "$(_RequireWindowsTraySignature)")),
            new XElement("Target", new XAttribute("Name", "_PublishNativeCliWithEmbeddedBundle"),
                new XAttribute("Condition", "'$(BuildDistributionPackages)' == 'false' and '$(SkipNativeCliWithEmbeddedBundle)' != 'true'"),
                Record("cli")),
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
                    new XElement("Error", new XAttribute("Condition", $"Exists('{Path.Combine(root, "fail-" + component)}')"),
                        new XAttribute("Text", "Failed publishing " + component)),
                    Record(component),
                    new XElement("WriteLinesToFile", new XAttribute("File", properties),
                        new XAttribute("Lines", $"{component}|{propertyRecord}"), new XAttribute("Overwrite", "false")),
                    new XElement("WriteLinesToFile", new XAttribute("File", Path.Combine(root, "cache-properties")),
                        new XAttribute("Lines", $"{component}|$(NUGET_PACKAGES)|$(RestorePackagesPath)|$(NuGetPackageRoot)"), new XAttribute("Overwrite", "false")),
                    payloadPath is null ? null : Write(payloadPath, component),
                    tasks))).Save(path);
        }
    }

    private static string CreateOrchestrationProject(string root, string rid)
    {
        var project = CreateProject(root, rid);
        var events = Path.Combine(root, "events");
        var signer = Path.Combine(root, "signer.proj");
        var archive = Path.Combine(root, "aspire-cli-final.tar.gz");
        var tray = Path.Combine(root, "tray.proj");
        var trayExecutable = Path.Combine(root, "artifacts", "tray", "Aspire Tray.app", "Contents", "MacOS", "aspire-tray");
        var trayArchive = Path.Combine(root, "custom signing path", "AspireTray.zip");
        var shipping = Path.Combine(root, "shipping");
        Directory.CreateDirectory(shipping);
        var dashboardPackage = Path.Combine(shipping, "dashboard.nupkg");
        var dcpPackage = Path.Combine(shipping, "dcp.nupkg");
        var dashboardPackProject = XDocument.Load(Path.Combine(root, "package.proj"));
        dashboardPackProject.Root!.Elements("Target").Single(target => target.Attribute("Name")!.Value == "Build")
            .Add(Write(dashboardPackage, "Dashboard package"));
        dashboardPackProject.Save(Path.Combine(root, "package.proj"));
        var dcpPackProject = XDocument.Load(Path.Combine(root, "dcp.proj"));
        dcpPackProject.Root!.Elements("Target").Single(target => target.Attribute("Name")!.Value == "Build")
            .Add(Write(dcpPackage, "DCP package"));
        dcpPackProject.Save(Path.Combine(root, "dcp.proj"));
        var signingPlugin = Path.Combine(root, "signing-plugin.props");
        // The official job installs plugins, but SigningEnabled is computed
        // only when a signing child imports plugin props, not in Bundle.proj.
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup", Property("MicroBuild_SigningEnabled", "true")))).Save(signingPlugin);
        var bundle = XDocument.Load(project);
        bundle.Root!.Element("PropertyGroup")!.Add(
            Property("_BuildScriptPath", Path.Combine(root, OperatingSystem.IsWindows() ? "build.cmd" : "build.sh")),
            Property("_TraySigningArchivePath", trayArchive),
            Property("_TraySigningProjectPath", signer));
        // Distribution packaging must be the only CLI publisher in this mode.
        bundle.Root.Add(new XElement("Target", new XAttribute("Name", "_PublishNativeCliWithEmbeddedBundle"),
            new XAttribute("Condition", "'$(BuildDistributionPackages)' == 'false' and '$(SkipNativeCliWithEmbeddedBundle)' != 'true'"),
            new XElement("Error", new XAttribute("Text", "The CLI must not publish twice."))));
        bundle.Save(project);
        var trayProject = XDocument.Load(tray);
        trayProject.Root!.Add(new XElement("Target", new XAttribute("Name", "ValidateChildPublishControls"),
            new XAttribute("BeforeTargets", "PrepareTraySigning;RestoreSignedTray"),
            new[] { "SkipAspireManagedPublish", "SkipDashboardPublish", "SkipTerminalHostPublish",
                "SkipAppHostServerPublish", "SkipTrayPublish", "SkipNativeCliWithEmbeddedBundle" }
                .Select(control => new XElement("Error", new XAttribute("Condition", $"'$({control})' != ''"),
                    new XAttribute("Text", "Tray signing children must not inherit " + control)))));
        trayProject.Root!.Add(Phase("PrepareTraySigning", "prepare-tray", Copy(trayExecutable, "$(TraySigningArchive)")),
            Phase("RestoreSignedTray", "restore-tray", Copy("$(TraySigningArchive)", "$(TrayAppPath)/Contents/MacOS/aspire-tray")));
        trayProject.Save(tray);
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup",
                Property("ArtifactsBinDir", Path.Combine(root, "artifacts", "bin") + Path.DirectorySeparatorChar),
                Property("ArtifactsPackagesDir", root + Path.DirectorySeparatorChar),
                Property("ArtifactsShippingPackagesDir", Path.Combine(root, "shipping") + Path.DirectorySeparatorChar),
                Property("VisualStudioSetupInsertionPath", Path.Combine(root, "insertion"))),
            new XElement("Import", new XAttribute("Project", signingPlugin),
                new XAttribute("Condition", $"Exists('{signingPlugin}')")),
            // Exercise the production evaluation-time archive glob, not a
            // second synthetic discovery rule that could mask stale evaluation.
            new XElement("Import", new XAttribute("Project", Path.Combine(RepoRoot.Path, "eng", "Signing.props"))),
            new XElement("ItemGroup",
                new XElement("ArchiveToSign",
                    new XAttribute("Include", $"@(ItemsToSign->WithMetadataValue('FullPath', '{archive}'))")),
                // Tiny fixture packages stand in for Arcade's package signing.
                // Discovery must still happen in a new process after they exist.
                new XElement("PayloadPackageToSign", new XAttribute("Include", Path.Combine(shipping, "*.nupkg")))),
            new XElement("Target", new XAttribute("Name", "Restore")),
            new XElement("Target", new XAttribute("Name", "ValidateSigningPlugin"),
                new XAttribute("BeforeTargets", "SignComponents;SignTray;SignArchive;SignPackages"),
                new XAttribute("Condition", "'$(DotNetSignType)' == 'real'"),
                new XElement("Error", new XAttribute("Condition", "'$(MicroBuild_SigningEnabled)' != 'true'"),
                    new XAttribute("Text", "Signing child requires the MicroBuild plugin."))),
            Phase("SignComponents", "sign-components",
                Write(Path.Combine(root, "component-archive-exists"), "$([System.IO.File]::Exists('" + archive + "'))"),
                Write(DashboardPath(root, rid), "signed Dashboard"),
                Write(AppHostServerPath(root, rid), "signed AppHost Server"),
                new XElement("Exec", new XAttribute("Condition", "!$([MSBuild]::IsOSPlatform('Windows')) and $(TargetRid.StartsWith('osx-'))"),
                    new XAttribute("Command", $"chmod 644 \"{ManagedPath(root, rid)}\" \"{DashboardPath(root, rid)}\" \"{TerminalHostPath(root, rid)}\" \"{AppHostServerPath(root, rid)}\""))),
            Phase("SignTray", "sign-tray", Write("$(TraySigningArchive)", "signed tray")),
            Phase("NotarizeTray", "notarize-tray"),
            Phase("BuildCli", "cli",
                new XElement("Error", new XAttribute("Condition", "'$(SkipManagedBuild)' != 'true'"),
                    new XAttribute("Text", "Final CLI packaging must not rebuild managed components.")),
                new XElement("Error", new XAttribute("Condition", "'$(SkipAppHostServerBuild)' != 'true'"),
                    new XAttribute("Text", "Final CLI packaging must preserve the signed AppHost Server.")),
                new XElement("Error", new XAttribute("Condition", "'$(SkipNativeBuild)' != ''"),
                    new XAttribute("Text", "Do not inherit SkipNativeBuild.")),
                Copy("$(BundlePayloadPath)", Path.Combine(root, "cli-payload")),
                Write(Path.Combine(root, "cli-properties"), "$(Configuration)|$(TargetRids)|$(VersionSuffix)|$(OfficialBuildId)|$(AspireCliChannel)|$(SkipNativeBuild)|$(RestorePackagesPath)"),
                Write(archive, "CLI archive")),
            Phase("SignArchive", "sign-archive",
                new XElement("Error", new XAttribute("Condition", "'@(ArchiveToSign)' == ''"),
                    new XAttribute("Text", "Fresh signing evaluation must discover the final archive.")),
                Write(archive, "signed CLI archive")),
            // This substitutes only for orchestration; it does not execute the
            // production tool/npm extraction from the canonical signed archive.
            Phase("PackCli", "cli-pack", Copy(archive, Path.Combine(root, "cli-package"))),
            Phase("SignPackages", "sign-packages",
                new XElement("Error", new XAttribute("Condition", "'@(PayloadPackageToSign)' == ''"),
                    new XAttribute("Text", "Fresh signing evaluation must discover payload packages.")),
                Write(dashboardPackage, "signed Dashboard package"),
                new XElement("WriteLinesToFile", new XAttribute("Condition", $"Exists('{dcpPackage}')"),
                    new XAttribute("File", dcpPackage), new XAttribute("Lines", "signed DCP package"),
                    new XAttribute("Overwrite", "true")))))
            .Save(signer);
        File.WriteAllText(Path.Combine(root, "build.ps1"), $$"""
            $ErrorActionPreference = 'Stop'
            $BuildArguments = $args
            $properties = @($BuildArguments | Where-Object { $_.StartsWith('/p:') })
            $project = Join-Path $PSScriptRoot 'signer.proj'
            $targets = if ($BuildArguments -contains '-build') {
                @('BuildCli') + $(if ($BuildArguments -contains '-sign') { 'SignArchive' }) + @('PackCli')
            } elseif ($BuildArguments | Where-Object { $_ -like '*/bl:*SignBundlePayloadPackages.binlog' }) {
                @('SignPackages')
            } else { @('SignComponents') }
            foreach ($target in $targets) {
                & dotnet msbuild $project "-t:$target" @properties
                if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            }
            """);
        // SignTray's real implementation signs and then notarizes. Keep those
        // operations independently fallible in the small subprocess fixture.
        var signerProject = XDocument.Load(signer);
        signerProject.Root!.Elements("Target").Single(target => target.Attribute("Name")!.Value == "SignTray")
            .Add(new XElement("CallTarget", new XAttribute("Targets", "NotarizeTray")));
        signerProject.Save(signer);
        var tools = Path.Combine(root, "bin");
        Directory.CreateDirectory(tools);
        File.WriteAllText(Path.Combine(root, "codesign.ps1"), $$"""
            $ErrorActionPreference = 'Stop'
            if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'fail-adhoc')) { exit 1 }
            Add-Content -LiteralPath (Join-Path $PSScriptRoot 'events') -Value 'adhoc'
            """);
        WriteLauncher(Path.Combine(root, OperatingSystem.IsWindows() ? "build.cmd" : "build.sh"), Path.Combine(root, "build.ps1"));
        WriteLauncher(Path.Combine(tools, OperatingSystem.IsWindows() ? "codesign.cmd" : "codesign"), Path.Combine(root, "codesign.ps1"));
        return project;

        XElement Fail(string phase) => new("Error", new XAttribute("Condition", $"Exists('{Path.Combine(root, "fail-" + phase)}')"),
            new XAttribute("Text", "Failed " + phase));

        XElement Phase(string name, string phase, params XElement[] tasks) => new("Target",
            new XAttribute("Name", name), Fail(phase),
            new XElement("WriteLinesToFile", new XAttribute("File", events),
                new XAttribute("Lines", phase), new XAttribute("Overwrite", "false")),
            new XElement("WriteLinesToFile", new XAttribute("File", Path.Combine(root, "cache-properties")),
                new XAttribute("Lines", $"{phase}|$(NUGET_PACKAGES)|$(RestorePackagesPath)|$(NuGetPackageRoot)"), new XAttribute("Overwrite", "false")), tasks);

        static void WriteLauncher(string path, string script)
        {
            File.WriteAllText(path, OperatingSystem.IsWindows()
                ? $"@pwsh -NoProfile -File \"{script}\" %*\r\n"
                : $"#!/bin/sh\nexec pwsh -NoProfile -File \"{script}\" \"$@\"\n");
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
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

    private static string AppHostServerPath(string root, string rid) =>
        Path.Combine(root, "artifacts", "bin", "Aspire.Hosting.Native.Server", "Release", "net11.0", rid, "publish",
            rid.StartsWith("win-", StringComparison.Ordinal) ? "Aspire.Hosting.Native.Server.exe" : "Aspire.Hosting.Native.Server");

    private async Task RunAsync(string project, string target, string rid, params string[] properties)
    {
        var result = await ExecuteAsync(project, target, rid, properties);
        Assert.True(result.ExitCode == 0, result.Output);
    }

    private Task<(int ExitCode, string Output)> ExecuteAsync(string project, string target, string rid, params string[] properties) =>
        ExecuteAsync(project, target, rid, new Dictionary<string, string?>(), properties);

    private async Task<(int ExitCode, string Output)> ExecuteAsync(string project, string target, string rid,
        IReadOnlyDictionary<string, string?> environment, params string[] properties)
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
        start.Environment["PATH"] = Path.Combine(Path.GetDirectoryName(project)!, "bin") + Path.PathSeparator
            + Path.GetDirectoryName(dotnet) + Path.PathSeparator + start.Environment["PATH"];
        foreach (var (name, value) in environment)
        {
            if (value is null)
            {
                start.Environment.Remove(name);
            }
            else
            {
                start.Environment[name] = value;
            }
        }
        foreach (var argument in new[] { "msbuild", project, "-nologo", "-p:Configuration=Release",
            "-p:ContinuousIntegrationBuild=true", $"-p:TargetRid={rid}", "-p:VersionPrefix=42.0.0",
            "-p:VersionSuffix=ci.test", "-p:OfficialBuildId=20261014.1", "-p:AspireCliChannel=staging",
            "-p:DotNetFinalVersionKind=release", "-p:StabilizePackageVersion=true", "-p:IsOfficialBuild=true" }
            .Concat(properties.Select(property => "-p:" + property)))
        {
            start.ArgumentList.Add(argument);
        }
        if (target.Length != 0)
        {
            start.ArgumentList.Add($"-t:{target}");
        }
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
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
