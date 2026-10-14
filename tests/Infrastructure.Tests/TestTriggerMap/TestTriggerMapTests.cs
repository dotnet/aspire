// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.SelectTests;
using Aspire.TestUtilities;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Runtime.CompilerServices;
using Xunit;

namespace Infrastructure.Tests.TestTriggerMap;

/// <summary>
/// Keeps the curated <c>eng/github-ci/test-trigger-map.yml</c> honest against repo reality.
/// The map is hand-maintained, so these tests fail loudly when a project/job is renamed
/// or removed, when a curated path is typo'd, or when a new source project is added that
/// no rule maps to.
/// </summary>
[Collection("GraphAffectedProjects")] // MSBuildLocator registers process-wide; keep graph use serialized.
public sealed class TestTriggerMapTests
{
    private static readonly TestTriggerMap s_map = TestTriggerMap.Load(RepoRoot.Path);

    // Repo-relative, '/'-separated tracked file paths (git ls-files). Source of truth for
    // "does this glob match a real file" and "what source projects exist". Loaded once.
    private static readonly IReadOnlyList<string> s_trackedFiles = LoadTrackedFiles();

    [Fact]
    public void MapLoadsWithExpectedVersion()
    {
        Assert.Equal(1, s_map.Version);
    }

    [Theory]
    [InlineData("eng/WarningPolicy.proj")]
    [InlineData("eng/build.ps1")]
    [InlineData("eng/build.sh")]
    [InlineData("build.sh")]
    [InlineData("restore.sh")]
    public void WarningPolicyChangesSelectAllTests(string path)
    {
        var targets = s_map.PathRules
            .Where(rule => rule.Paths.Any(glob => TestTriggerMap.GlobMatches(glob, path)))
            .SelectMany(rule => rule.Targets)
            .Distinct()
            .ToArray();

        Assert.Equal(["ALL"], targets);
    }

    [Fact]
    public void MtpExitCodeNormalizerRuleSelectsAllConsumers()
    {
        var rule = Assert.Single(
            s_map.PathRules,
            rule => rule.Paths.Any(path => path.Contains("normalize-mtp-exit-code", StringComparison.Ordinal)));

        Assert.Equal(["ALL", "job:deployment-e2e"], rule.Targets);
    }

    [Fact]
    public void DeploymentTestRunnerSelectsDeploymentWorkflow()
    {
        const string path = ".github/workflows/run-deployment-test.sh";
        var targets = s_map.PathRules
            .Where(rule => rule.Paths.Any(glob => TestTriggerMap.GlobMatches(glob, path)))
            .SelectMany(rule => rule.Targets);

        Assert.Contains("job:deployment-e2e", targets);
        Assert.Contains("test:Infrastructure.Tests", targets);
    }

    [Fact]
    public void ExtensionUnitWorkflowChangesSelectUnitAndE2eJobs()
    {
        const string workflow = ".github/workflows/extension-unit-tests.yml";
        var targets = s_map.PathRules
            .Where(rule => rule.Paths.Any(path => TestTriggerMap.GlobMatches(path, workflow)))
            .SelectMany(rule => rule.Targets)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("job:extension-unit", targets);
        Assert.Contains("job:extension-e2e", targets);
    }

    [Fact]
    public void ExtensionE2eProjectAndPathRulesCoverTheSameConsumers()
    {
        var affectedProjects = Assert.Single(
            s_map.AffectedProjectRules,
            rule => rule.Targets.Contains("job:extension-e2e", StringComparer.Ordinal))
            .Projects
            .Select(project => $"src/{project}/**");
        var directPaths = Assert.Single(
            s_map.PathRules,
            rule => rule.Targets.Contains("job:extension-e2e", StringComparer.Ordinal)
                && rule.Paths.Contains("extension/**", StringComparer.Ordinal))
            .Paths
            .Where(path => path.StartsWith("src/", StringComparison.Ordinal));

        Assert.Equal(
            affectedProjects.Order(StringComparer.Ordinal),
            directPaths.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RulesAreWellFormed()
    {
        // Structural hygiene: a path rule with no globs, or globs but no targets, is dead weight
        // that silently selects nothing. (Overlapping globs ACROSS path_rules are expected and
        // fine -- one path may map to several targets via several rules -- so there is no
        // duplicate-glob check here.)
        var problems = new List<string>();

        foreach (var rule in s_map.PathRules)
        {
            var label = rule.Paths.Count > 0 ? rule.Paths[0] : "(no paths)";
            if (rule.Paths.Count == 0 || rule.Paths.Any(string.IsNullOrWhiteSpace))
            {
                problems.Add($"rule '{label}' has an empty path glob");
            }
            if (rule.Targets.Count == 0 || rule.Targets.Any(string.IsNullOrWhiteSpace))
            {
                problems.Add($"rule '{label}' has no targets");
            }
        }

        // conventions: each entry needs a pattern with a <name> placeholder and a target that also
        // carries <name> (so the capture is actually substituted). Duplicate patterns are slips.
        foreach (var convention in s_map.Conventions)
        {
            if (string.IsNullOrWhiteSpace(convention.Pattern) || !convention.Pattern.Contains("<name>", StringComparison.Ordinal))
            {
                problems.Add($"conventions entry '{convention.Pattern}' has no <name> placeholder");
            }
            if (string.IsNullOrWhiteSpace(convention.Target) || !convention.Target.Contains("<name>", StringComparison.Ordinal))
            {
                problems.Add($"conventions pattern '{convention.Pattern}' has a target that does not substitute <name>");
            }
        }

        var dupeConventionPatterns = s_map.Conventions
            .GroupBy(c => c.Pattern, StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key).Order(StringComparer.Ordinal).ToList();
        if (dupeConventionPatterns.Count > 0)
        {
            problems.Add($"duplicate conventions patterns: {string.Join(", ", dupeConventionPatterns)}");
        }

        // ignore globs must be non-empty.
        if (s_map.Ignore.Any(string.IsNullOrWhiteSpace))
        {
            problems.Add("an ignore glob is empty");
        }

        // derived_targets: each entry needs at least one test: trigger and at least one target.
        foreach (var derived in s_map.DerivedTargets)
        {
            if (derived.Tests.Count == 0 || derived.Tests.Any(t => string.IsNullOrWhiteSpace(t) || !t.StartsWith("test:", StringComparison.Ordinal)))
            {
                problems.Add($"derived_targets entry has an invalid/empty tests list: [{string.Join(", ", derived.Tests)}]");
            }
            if (derived.Targets.Count == 0 || derived.Targets.Any(string.IsNullOrWhiteSpace))
            {
                problems.Add($"derived_targets entry [{string.Join(", ", derived.Tests)}] has no targets");
            }
        }

        // affected_project_rules: each entry needs at least one project name glob and at least one target.
        foreach (var rule in s_map.AffectedProjectRules)
        {
            var label = rule.Projects.Count > 0 ? rule.Projects[0] : "(no projects)";
            if (rule.Projects.Count == 0 || rule.Projects.Any(string.IsNullOrWhiteSpace))
            {
                problems.Add($"affected_project_rules entry '{label}' has an empty project glob");
            }
            if (rule.Targets.Count == 0 || rule.Targets.Any(string.IsNullOrWhiteSpace))
            {
                problems.Add($"affected_project_rules entry '{label}' has no targets");
            }
        }

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void EverySourceProjectIsReachableByLayer1OrACuratedRule()
    {
        // The graph closure is owned by the Layer 1 graph (GraphAffectedProjects), which discovers
        // projects from Aspire.slnx. So a src project is "covered" if it is in the solution (∴ Layer 1 sees it)
        // OR matched by a curated glob (the deliberately out-of-slnx ones — e.g. the template
        // placeholders that crash discovery — are covered by loose_file_deps). A new src project
        // that is neither in the solution nor curated would silently never run any test, so it
        // must fail here.
        var inSolution = LoadSolutionProjectPaths();

        var selecting = new Matcher(StringComparison.Ordinal);
        foreach (var glob in s_map.AllSelectingGlobs())
        {
            selecting.AddInclude(glob);
        }
        var curatedCovered = selecting.Match(s_trackedFiles).Files
            .Select(f => f.Path)
            .ToHashSet(StringComparer.Ordinal);

        var uncovered = s_trackedFiles
            .Where(f => f.StartsWith("src/", StringComparison.Ordinal) && f.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(csproj => !inSolution.Contains(csproj) && !curatedCovered.Contains(csproj))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(uncovered.Count == 0,
            $"src projects neither in Aspire.slnx nor matched by a curated rule: {string.Join(", ", uncovered)}");
    }

    // Repo-relative '/'-separated project paths listed in Aspire.slnx (the Layer 1 graph root).
    private static IReadOnlySet<string> LoadSolutionProjectPaths()
    {
        var slnx = File.ReadAllText(Path.Combine(RepoRoot.Path, "Aspire.slnx"));
        // <Project Path="src/Foo/Foo.csproj" /> — paths use '/' in the slnx already.
        return System.Text.RegularExpressions.Regex.Matches(slnx, "Path=\"([^\"]+\\.csproj)\"")
            .Select(m => m.Groups[1].Value.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
    }

    [Fact]
    public void EveryTestProjectIsInTheSolutionSoLayer1CanSelectIt()
    {
        // A matrix test project that is NOT in Aspire.slnx is invisible to Layer 1 (the graph only
        // walks the solution), so a change to a production dependency could never fan into it --
        // it would silently never run in enforcing mode. Require every tests/<Name>/<Name>.csproj to
        // be in the solution. (This invariant is what the Infrastructure.Tests / Aspire.Hosting.Maui
        // .Tests additions satisfied; before them, both were silent Layer-1 blind spots.) Add to the
        // allow-list only for a project deliberately kept out of PR CI, with a reason.
        var allowList = new HashSet<string>(StringComparer.Ordinal)
        {
            // (none today)
        };

        var inSolution = LoadSolutionProjectPaths();
        var testsRoot = Path.Combine(RepoRoot.Path, "tests");

        var missing = Directory.EnumerateDirectories(testsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => $"tests/{name}/{name}.csproj")
            .Where(rel => File.Exists(Path.Combine(RepoRoot.Path, rel)))
            .Where(rel => !inSolution.Contains(rel) && !allowList.Contains(rel))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"test projects not in Aspire.slnx (Layer 1 cannot select them, so a production-dependency " +
            $"change would silently skip their tests): {string.Join(", ", missing)}");
    }

    [Fact]
    public void ProvisioningProjectsSelectPolyglotJob()
    {
        var projects = LoadSolutionProjectPaths()
            .Where(path => path.StartsWith("src/Aspire.Hosting.Azure.Provisioning", StringComparison.Ordinal))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToArray();

        Assert.NotEmpty(projects);
        Assert.All(projects, project =>
        {
            var targets = s_map.AffectedProjectRules
                .Where(rule => rule.Projects.Any(pattern =>
                    System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, project, ignoreCase: false)))
                .SelectMany(rule => rule.Targets)
                .ToHashSet(StringComparer.Ordinal);

            Assert.Contains("job:polyglot", targets);
        });
    }

    [Fact]
    public void EveryAffectedProjectRuleGlobMatchesASolutionProject()
    {
        // affected_project_rules key off the affected PROJECT set (Layer 1), matched by project-name
        // glob. Layer 1 can only ever report a project that is in Aspire.slnx, so a project glob
        // that matches no solution project name would silently select nothing — assert each matches
        // at least one. Project Name == the .csproj base name (what Layer 1 emits).
        var solutionProjectNames = LoadSolutionProjectPaths()
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .ToHashSet(StringComparer.Ordinal);

        var dead = s_map.AffectedProjectRules
            .SelectMany(r => r.Projects)
            .Distinct(StringComparer.Ordinal)
            .Where(pattern => !solutionProjectNames.Any(name =>
                System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: false)))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(dead.Count == 0,
            $"affected_project_rules globs matching no project in Aspire.slnx: {string.Join(", ", dead)}");
    }

    [Fact]
    public void EveryGlobMatchesAtLeastOneTrackedFile()
    {
        // Every rule glob (catch-all, path-rule paths, convention patterns, ignore globs) must match
        // a real, git-tracked file. A typo'd path or a renamed/removed source folder would otherwise
        // sit in the map selecting nothing — a silent hole the selector can't see.
        var globs = s_map.AllSelectingGlobs()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        var deadGlobs = globs.Where(g => !GlobMatchesAnyTrackedFile(g)).ToList();

        Assert.True(deadGlobs.Count == 0, $"globs matching no tracked file: {string.Join(", ", deadGlobs)}");
    }

    private static bool GlobMatchesAnyTrackedFile(string glob)
    {
        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddInclude(glob);
        return matcher.Match(s_trackedFiles).HasMatches;
    }

    private static IReadOnlyList<string> LoadTrackedFiles()
    {
        var output = GitCli.Run(RepoRoot.Path, "ls-files");
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    [Fact]
    public void GroupsResolveToValidTargets()
    {
        // Named groups (e.g. CLI_BUNDLE) expand into concrete test:/job: targets by a consumer.
        // Each member must be a well-formed test: or job: target (existence is covered by
        // EveryTestTargetNamesAnExistingTestProject / EveryJobTargetMapsToAnExistingWorkflowOrJob,
        // which include group members). The real map keeps groups flat (no nesting); the selector
        // engine supports recursive expansion, exercised by the synthetic acceptance tests.
        foreach (var (name, members) in s_map.Groups)
        {
            Assert.True(members.Count > 0, $"group {name} is empty");

            var bad = members.Where(m => !m.StartsWith("test:", StringComparison.Ordinal) && !m.StartsWith("job:", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal).ToList();
            Assert.True(bad.Count == 0, $"group {name} has members that are neither test: nor job:: {string.Join(", ", bad)}");

            var dupes = members.GroupBy(m => m, StringComparer.Ordinal)
                .Where(g => g.Count() > 1).Select(g => g.Key).Order(StringComparer.Ordinal).ToList();
            Assert.True(dupes.Count == 0, $"group {name} has duplicate members: {string.Join(", ", dupes)}");
        }

        // Every group-like token used as a target (uppercase, not test:/job:) is either a
        // selector sentinel or a defined group — so a typo'd group reference fails loudly.
        var undefined = s_map.AllReferencedTargets()
            .Where(t => !t.StartsWith("test:", StringComparison.Ordinal) && !t.StartsWith("job:", StringComparison.Ordinal))
            .Where(t => t is not "ALL" and not "DOTNET_TESTS" && !s_map.Groups.ContainsKey(t))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        Assert.True(undefined.Count == 0, $"undefined group references: {string.Join(", ", undefined)}");
    }

    public static TheoryData<string, string[]> AuditedLoosePathCases => new()
    {
        {
            "eng/scripts/pack-cli-npm-package.ps1",
            ["test:Aspire.Cli.Tests", "test:Infrastructure.Tests"]
        },
        {
            "tools/CreateFailingTestIssue/Program.cs",
            ["test:Infrastructure.Tests"]
        },
        {
            "tools/CreateFailingTestIssue/CreateFailingTestIssue.csproj",
            ["test:Infrastructure.Tests"]
        },
        {
            ".github/workflows/prepare-installer-artifacts.yml",
            ["job:winget-installer", "job:homebrew-installer"]
        },
        {
            ".github/scripts/assert-extension-e2e-bridge-vsix.ps1",
            ["job:extension-unit"]
        },
        {
            ".vscode/launch.json",
            ["job:extension-unit"]
        },
        {
            ".vscode/tasks.json",
            ["job:extension-unit"]
        },
        {
            "eng/test-retry-patterns.json",
            ["test:Infrastructure.Tests"]
        },
        {
            "eng/scripts/aspire-skills-bundle.common.ps1",
            ["test:Infrastructure.Tests"]
        },
        {
            "eng/scripts/smoke-installed-cli.ps1",
            ["job:winget-installer"]
        },
        {
            "eng/scripts/smoke-installed-cli.sh",
            ["job:homebrew-installer"]
        },
        {
            // These paths do not exist intentionally: they prove new packaging inputs stay covered
            // without requiring this test to enumerate every current RID-specific project.
            "eng/dashboardpack/future-package-input.targets",
            [
                "test:Aspire.Hosting.Sdk.Tests",
                "test:Aspire.Templates.Tests",
                "test:Aspire.Cli.EndToEnd.Tests",
                "job:cli-starter-validation",
                "job:extension-e2e",
                "job:homebrew-installer",
                "job:native-dashboard-validation",
                "job:winget-installer"
            ]
        },
        {
            "eng/dashboardpack/Sdk.targets",
            [
                "test:Aspire.Hosting.Sdk.Tests",
                "test:Aspire.Templates.Tests",
                "test:Aspire.Cli.EndToEnd.Tests",
                "job:cli-starter-validation",
                "job:extension-e2e",
                "job:homebrew-installer",
                "job:native-dashboard-validation",
                "job:winget-installer"
            ]
        },
        {
            "eng/dashboardpack/Common.projitems",
            [
                "test:Aspire.Hosting.Sdk.Tests",
                "test:Aspire.Templates.Tests",
                "test:Aspire.Cli.EndToEnd.Tests",
                "job:cli-starter-validation",
                "job:extension-e2e",
                "job:homebrew-installer",
                "job:native-dashboard-validation",
                "job:winget-installer"
            ]
        },
        {
            "eng/dcppack/future-package-input.targets",
            ["test:Aspire.Cli.EndToEnd.Tests", "test:Aspire.Hosting.Sdk.Tests", "test:Aspire.Templates.Tests", "test:Aspire.TerminalHost.Tests", "job:extension-e2e"]
        },
        {
            "eng/scripts/load-cli-e2e-images.sh",
            ["test:Aspire.Cli.EndToEnd.Tests"]
        },
        {
            "eng/clipack/Common.projitems",
            [
                "test:Aspire.Cli.Tests",
                "test:Aspire.Cli.EndToEnd.Tests",
                "job:cli-starter-validation",
                "job:extension-e2e",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/clipack/Aspire.Cli.win-x64.csproj",
            [
                "test:Aspire.Cli.Tests",
                "job:cli-starter-validation",
                "job:extension-e2e",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/clipack/Aspire.Cli.linux-musl-x64.csproj",
            [
                "test:Aspire.Cli.Tests",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/clipack/Aspire.Cli.linux-arm64.csproj",
            [
                "test:Aspire.Cli.Tests",
                "job:cli-starter-validation",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/clipack/npm/aspire.js",
            [
                "test:Aspire.Cli.Tests",
                "test:Infrastructure.Tests",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/clipack/Aspire.Cli.NativeSymbols.proj",
            [
                "test:Aspire.Cli.Tests",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/scripts/cli-starter-validation.ps1",
            ["job:cli-starter-validation"]
        },
        {
            "eng/scripts/test-native-dashboard.ps1",
            ["job:native-dashboard-validation"]
        },
        {
            "eng/scripts/verify-native-dashboard-warnings.ps1",
            ["job:native-dashboard-validation"]
        },
        {
            ".github/workflows/native-dashboard-validation.yml",
            ["job:native-dashboard-validation"]
        },
        {
            "tests/Aspire.Dashboard.Tests/Integration/Playwright/NativeAotDashboardTests.cs",
            ["test:Aspire.Dashboard.Tests", "job:native-dashboard-validation"]
        },
        {
            "eng/scripts/get-aspire-cli-pr.ps1",
            [
                "test:Aspire.Acquisition.Tests",
                "test:Aspire.Cli.EndToEnd.Tests",
                "job:cli-starter-validation",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            "eng/scripts/get-aspire-cli-pr.sh",
            [
                "test:Aspire.Acquisition.Tests",
                "test:Aspire.Cli.EndToEnd.Tests",
                "job:homebrew-installer",
                "job:winget-installer"
            ]
        },
        {
            ".github/workflows/cli-starter-validation.yml",
            ["job:cli-starter-validation"]
        },
        {
            "eng/clipack/Aspire.Cli.linux-x64.csproj",
            ["test:Aspire.Cli.Tests", "test:Aspire.Cli.EndToEnd.Tests", "job:cli-starter-validation", "job:extension-e2e", "job:winget-installer", "job:homebrew-installer"]
        },
        {
            "tools/CreateLayout/Program.cs",
            ["test:Aspire.Cli.EndToEnd.Tests", "job:cli-starter-validation", "job:extension-e2e", "job:winget-installer", "job:homebrew-installer"]
        },
        {
            "eng/Bundle.proj",
            ["test:Aspire.Cli.EndToEnd.Tests", "job:cli-starter-validation", "job:extension-e2e", "job:winget-installer", "job:homebrew-installer"]
        },
        {
            "playground/JavaSpringBoot/JavaSpringBoot.AppHost.Java/aspire.config.json",
            ["test:Aspire.Playground.Tests", "job:extension-e2e"]
        },
        {
            ".gitignore",
            ["test:Infrastructure.Tests"]
        },
        {
            ".github/scripts/install-extension-e2e-recorder.ps1",
            ["job:extension-e2e", "test:Infrastructure.Tests"]
        },
        {
            ".github/workflows/run-deployment-test.sh",
            ["job:deployment-e2e", "test:Infrastructure.Tests"]
        },
    };

    [Theory]
    [MemberData(nameof(AuditedLoosePathCases))]
    public void AuditedLoosePathSelectsExactConsumerSetWithoutRunAllFallback(string path, string[] expectedTargets)
    {
        var result = SelectWithRealMap(path);

        Assert.False(result.SelectsAll, $"{path} unexpectedly selected ALL: {result.EscalationReason}");
        Assert.Empty(result.UnmatchedFiles);

        var actualTargets = result.TestProjects.Select(name => $"test:{name}")
            .Concat(result.Jobs)
            .Order(StringComparer.Ordinal);
        Assert.Equal(expectedTargets.Order(StringComparer.Ordinal), actualTargets);
    }

    [Fact]
    [RequiresTools(["git"])]
    public void ExtensionJavaE2eDiscoveryInputsAreNotGitIgnored()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = RepoRoot.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--no-pager");
        startInfo.ArgumentList.Add("check-ignore");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("extension/java-e2e-workspace/AppHost.java");
        startInfo.ArgumentList.Add("extension/java-e2e-workspace/aspire.config.json");
        startInfo.ArgumentList.Add("extension/java-e2e-workspace/JavaSpringBoot.AppHost.Java/AppHost.java");
        startInfo.ArgumentList.Add("extension/java-e2e-workspace/JavaSpringBoot.AppHost.Java/aspire.config.json");

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git.");
        var ignoredPaths = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 1,
            process.ExitCode == 0
                ? $"Git ignore rules must not exclude Java AppHost discovery inputs because aspire ls discovers them through git: {ignoredPaths}"
                : $"git check-ignore failed ({process.ExitCode}): {stderr}");
    }

    [Theory]
    [InlineData("src/Aspire.Dashboard/Components/Layout/TerminalDock.razor.js")]
    [InlineData("src/Aspire.Dashboard/wwwroot/js/app-terminalwindow.js")]
    [InlineData("tests/Aspire.Dashboard.Components.Tests/JavaScript/TerminalWindow.test.mjs")]
    public void DashboardTerminalScriptInputsSelectInfrastructureTests(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        // Infrastructure executes the scripts in addition to the consumers attributed by the project graph.
        Assert.Contains("Infrastructure.Tests", result.TestProjects);
    }

    [Theory]
    [InlineData("eng/scripts/gha-testreport.ps1")]
    [InlineData("eng/scripts/split-test-projects-for-ci.ps1")]
    [InlineData("tools/ExtractTestPartitions/Program.cs")]
    public void BroadCiInputHasAnExplicitRunAllRule(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.True(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
    }

    [Fact]
    public void GitAttributesLineEndingRulesRunTheFullMatrix()
    {
        var result = SelectWithRealMap(".gitattributes");

        Assert.True(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
    }

    [Fact]
    public void SolutionEditRunsTheFullMatrix()
    {
        var soloEdit = SelectWithRealMap("Aspire.slnx");

        Assert.True(soloEdit.SelectsAll);
        Assert.Empty(soloEdit.UnmatchedFiles);

        // A project removed from the solution takes its files with it; nothing at head owns them, so the
        // run-all fallback still fires on the deleted project itself.
        var removal = SelectWithRealMap("src/Aspire.Hosting.Retired/Aspire.Hosting.Retired.csproj");

        Assert.True(removal.SelectsAll);
    }

    [Fact]
    public void SolutionFileHasAnExplicitAllRule()
    {
        var solutionRule = Assert.Single(
            s_map.PathRules,
            rule => rule.Paths.Contains("Aspire.slnx", StringComparer.Ordinal));

        Assert.Equal(["ALL"], solutionRule.Targets);
        Assert.DoesNotContain("Aspire.slnx", s_map.Ignore, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("eng/common/BuildConfiguration/build-configuration.json")]
    [InlineData("eng/common/loc/P22DotNetHtmlLocalization.lss")]
    [InlineData("eng/common/generate-locproject.ps1")]
    [InlineData("eng/common/native/NativeAotSupported.props")]
    public void EngCommonInputsRunTheFullMatrix(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.True(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
    }

    [Theory]
    [InlineData("eng/common/templates/jobs/jobs.yml")]
    [InlineData("eng/common/templates-official/jobs/jobs.yml")]
    [InlineData("eng/common/core-templates/jobs/jobs.yml")]
    public void EngCommonPipelineTemplatesAlsoRunTheFullMatrix(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.True(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
    }

    [Theory]
    [InlineData("eng/scripts/verify-cli-npm-package.ps1")]
    [InlineData("eng/scripts/verify-cli-tool-nupkg.ps1")]
    [InlineData("eng/scripts/stabilization-smoke-init-restore.sh")]
    [InlineData("eng/generate-catalog.ps1")]
    [InlineData("eng/scripts/update-aspire-skills-bundle.ps1")]
    [InlineData("eng/scripts/verify-aspire-skills-bundle.ps1")]
    public void PathHandledOutsideSelectorDoesNotForceTestSelection(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        Assert.Empty(result.TestProjects);
        Assert.Empty(result.Jobs);
    }

    [Theory]
    [InlineData("src/Aspire.Hosting.Dotnet/Aspire.Hosting.Dotnet.csproj", "Aspire.Hosting.Dotnet")]
    [InlineData("src/Aspire.Hosting.Foundry/Aspire.Hosting.Foundry.csproj", "Aspire.Hosting.Foundry")]
    public void DeploymentE2eIsSelectedWhenRuntimeHostingDependencyIsAffected(string path, string affectedProject)
    {
        var result = SelectWithRealMap(path, affectedProject);

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        Assert.Contains("job:deployment-e2e", result.Jobs);
        Assert.Contains(result.JobCauses["job:deployment-e2e"], cause =>
            cause.Kind == CauseKind.AffectedProject && cause.Trigger == affectedProject);
    }

    [Fact]
    public void DeploymentE2eIsNotSelectedForFoundryTestOnlyChange()
    {
        var result = SelectWithRealMap(
            "tests/Aspire.Hosting.Foundry.Tests/Aspire.Hosting.Foundry.Tests.csproj",
            "Aspire.Hosting.Foundry.Tests");

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        Assert.Equal("Aspire.Hosting.Foundry.Tests", Assert.Single(result.TestProjects));
        Assert.Empty(result.Jobs);
    }

    [Theory]
    [InlineData("src/Aspire.Cli/Commands/RunCommand.cs", "Aspire.Cli")]
    [InlineData("src/Aspire.Managed/Program.cs", "Aspire.Managed")]
    [InlineData("src/Aspire.AppHost.Sdk/Aspire.AppHost.Sdk.csproj", "Aspire.AppHost.Sdk")]
    [InlineData("src/Aspire.Hosting.JavaScript/JavaScriptAppResource.cs", "Aspire.Hosting.JavaScript")]
    [InlineData("src/Aspire.Hosting.CodeGeneration.TypeScript/TypeScriptApiProjector.cs", "Aspire.Hosting.CodeGeneration.TypeScript")]
    [InlineData("src/Aspire.Hosting.AppHost/Aspire.Hosting.AppHost.csproj", "Aspire.Hosting.AppHost")]
    [InlineData("src/Aspire.Hosting.PostgreSQL/PostgresBuilderExtensions.cs", "Aspire.Hosting.PostgreSQL")]
    public void CliStarterValidationRunsWhenStarterRuntimeDependencyIsAffected(string path, string affectedProject)
    {
        var result = SelectWithRealMap(path, affectedProject);

        Assert.False(result.SelectsAll);
        Assert.Contains("job:cli-starter-validation", result.Jobs);
        Assert.Contains(result.JobCauses["job:cli-starter-validation"], cause => cause.Kind == CauseKind.AffectedProject);
    }

    [Theory]
    [InlineData("src/Aspire.ProjectTemplates/Aspire.ProjectTemplates.csproj")]
    [InlineData("src/Aspire.ProjectTemplates/templates/aspire-starter/Aspire-StarterApplication.1.AppHost/AppHost.cs")]
    [InlineData("eng/Bundle.proj")]
    public void CliStarterValidationRunsForLooseStarterConsumerChange(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.False(result.SelectsAll);
        Assert.Contains("job:cli-starter-validation", result.Jobs);
    }

    [Theory]
    [InlineData("src/Aspire.ProjectTemplates/Aspire.ProjectTemplates.csproj")]
    // This path does not exist intentionally: it proves a newly added template is covered by the
    // package-wide rule instead of repeating the current template inventory in the test.
    [InlineData("src/Aspire.ProjectTemplates/templates/future-template/.template.config/template.json")]
    public void CliE2eRunsForProjectTemplatePackageInputs(string path)
    {
        var result = SelectWithRealMap(path);

        Assert.False(result.SelectsAll);
        Assert.Contains("Aspire.Cli.EndToEnd.Tests", result.TestProjects);
    }

    [Fact]
    public void CliBundleConsumersRunWhenCreateLayoutIsAffected()
    {
        var result = SelectWithRealMap("tools/CreateLayout/Program.cs", "CreateLayout");

        Assert.False(result.SelectsAll);
        Assert.Contains("Aspire.Cli.EndToEnd.Tests", result.TestProjects);
        Assert.Contains(
            result.TestCauses["Aspire.Cli.EndToEnd.Tests"],
            cause => cause.Kind == CauseKind.AffectedProject);
        Assert.Equal(
            ["job:cli-starter-validation", "job:extension-e2e", "job:homebrew-installer", "job:winget-installer"],
            result.Jobs.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void DashboardChangesRunTemplatesTestsThatStartAppHosts()
    {
        var result = SelectWithRealMap(
            "src/Aspire.Dashboard/Program.cs",
            "Aspire.Dashboard");

        Assert.False(result.SelectsAll);
        Assert.Contains("Aspire.Templates.Tests", result.TestProjects);
        Assert.Contains("job:native-dashboard-validation", result.Jobs);
    }

    [Fact]
    public void CliStarterValidationDoesNotRunForUnrelatedHostingIntegrationChange()
    {
        var result = SelectWithRealMap(
            "src/Aspire.Hosting.Redis/RedisBuilderExtensions.cs",
            "Aspire.Hosting.Redis",
            "Aspire.Hosting.Redis.Tests",
            "Aspire.Hosting.Tests");

        Assert.False(result.SelectsAll);
        Assert.False(
            result.Jobs.Contains("job:cli-starter-validation", StringComparer.Ordinal),
            "Aggregate tests selected by Redis changes must not pull in all six starter-validation jobs.");
    }

    [Fact]
    public void CliStarterValidationDoesNotRunForNpgsqlClientChange()
    {
        var result = SelectWithRealMap(
            "src/Components/Aspire.Npgsql/NpgsqlCommon.cs",
            "Aspire.Npgsql",
            "Aspire.Npgsql.Tests",
            "Aspire.Hosting.PostgreSQL.Tests");

        Assert.False(result.SelectsAll);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public void UnrelatedProjectTemplateChangeRunsCliE2eAndTemplatesTestsWithoutStarterValidation()
    {
        var result = SelectWithRealMap(
            "src/Aspire.ProjectTemplates/templates/aspire-xunit/.template.config/template.json");

        Assert.False(result.SelectsAll);
        Assert.Equal(
            ["Aspire.Cli.EndToEnd.Tests", "Aspire.Templates.Tests"],
            result.TestProjects.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["job:deployment-e2e", "job:homebrew-installer", "job:winget-installer"],
            result.Jobs.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CliStarterValidationDoesNotRunForSharedTestUtilityChange()
    {
        var result = SelectWithRealMap(
            "tests/Aspire.TestUtilities/FileUtil.cs",
            "Aspire.TestUtilities",
            "Aspire.Cli.Tests",
            "Aspire.Cli.EndToEnd.Tests");

        Assert.False(result.SelectsAll);
        Assert.False(
            result.Jobs.Contains("job:cli-starter-validation", StringComparer.Ordinal),
            "Shared test utilities must not pull in all six starter-validation jobs.");
    }

    [Fact]
    public void CliStarterValidationDoesNotRunForHostingSdkTestFakeChange()
    {
        var result = SelectWithRealMap(
            "tests/Aspire.Hosting.Sdk.Tests.FakeCommand/Program.cs",
            "Aspire.Hosting.Sdk.Tests.FakeCommand",
            "Aspire.Hosting.Sdk.Tests");

        Assert.False(result.SelectsAll);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public void BuildPackagesWorkflowSelectsOnlyNuGetArtifactConsumers()
    {
        var result = SelectWithRealMap(".github/workflows/build-packages.yml");

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        Assert.Equal(
            ["Aspire.Cli.EndToEnd.Tests", "Aspire.Templates.Tests"],
            result.TestProjects.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["job:cli-starter-validation", "job:extension-e2e", "job:homebrew-installer", "job:polyglot", "job:winget-installer"],
            result.Jobs.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void BuildCliNativeArchivesWorkflowSelectsOnlyCliArchiveConsumers()
    {
        var result = SelectWithRealMap(".github/workflows/build-cli-native-archives.yml");

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        Assert.Equal(
            ["Aspire.Cli.EndToEnd.Tests", "Aspire.Templates.Tests"],
            result.TestProjects.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["job:cli-starter-validation", "job:extension-e2e", "job:homebrew-installer", "job:native-dashboard-validation", "job:polyglot", "job:winget-installer"],
            result.Jobs.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(".github/workflows/build-packages.yml")]
    [InlineData(".github/workflows/build-cli-native-archives.yml")]
    public void ArtifactProducerWorkflowSelectsEveryArtifactConsumingTestProject(string path)
    {
        // The narrowed routing above is only safe while the producer consumer groups still list every test
        // project that consumes a produced artifact in regular PR CI. Those projects declare artifact
        // consumption via RequiresNugets / RequiresCliArchive (read by eng/testing/CITestsProperties.props
        // into the runsheet, which is what puts them in the tests_requires_nugets_* /
        // tests_requires_cli_archive buckets that depend on the producer jobs). Adding a new regular-PR
        // consumer without updating the group would silently stop running it for producer-workflow changes
        // -- exactly the under-selection selective CI must never do.
        var consumers = LoadArtifactConsumingTestProjects()
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(consumers);

        var result = SelectWithRealMap(path);
        foreach (var consumer in consumers)
        {
            Assert.Contains(consumer, result.TestProjects, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void ArtifactProducerWorkflowExcludesOnlyKnownOuterloopArtifactConsumer()
    {
        var skippedConsumers = LoadArtifactConsumingTestProjectMetadata()
            .Where(project => project.SkipTests)
            .Select(project => project.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["Aspire.EndToEnd.Tests"], skippedConsumers);

        var packageConsumerGroup = Assert.Contains("BUILD_PACKAGE_CONSUMERS", s_map.Groups);
        var cliArchiveConsumerGroup = Assert.Contains("CLI_ARCHIVE_CONSUMERS", s_map.Groups);
        Assert.DoesNotContain("test:Aspire.EndToEnd.Tests", packageConsumerGroup, StringComparer.Ordinal);
        Assert.DoesNotContain("test:Aspire.EndToEnd.Tests", cliArchiveConsumerGroup, StringComparer.Ordinal);
    }

    [Fact]
    public void EveryTestTargetNamesAnExistingTestProject()
    {
        // test:<X> means the matrix project tests/<X> (run-tests.yml entry). A rename or
        // removal that the curated map misses would silently stop selecting that project,
        // so require tests/<X>/<X>.csproj to exist on disk.
        var missing = s_map.AllReferencedTargets()
            .Where(t => t.StartsWith("test:", StringComparison.Ordinal))
            .Select(t => t["test:".Length..])
            .Distinct(StringComparer.Ordinal)
            .Where(name => !File.Exists(Path.Combine(RepoRoot.Path, "tests", name, $"{name}.csproj")))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, $"test: targets with no tests/<X>/<X>.csproj: {string.Join(", ", missing)}");
    }

    [Fact]
    public void PrefilterPatternsFileExists()
    {
        // The prefilter reads its pattern list from this file at runtime (SSOT with the ci.yml skip
        // gate). A typo'd path would make ChangedFileFilter.Create throw at runtime; pin it here.
        Assert.NotNull(s_map.Prefilter);
        Assert.False(string.IsNullOrWhiteSpace(s_map.Prefilter!.PatternsFile));
        Assert.True(File.Exists(Path.Combine(RepoRoot.Path, s_map.Prefilter.PatternsFile!)),
            $"prefilter.patterns_file does not exist: {s_map.Prefilter.PatternsFile}");
    }

    [Fact]
    public void PrefilterKeepRoutedGlobsAreLiveAndCoverSelectorRoutedDirs()
    {
        // keep_routed are the carve-outs: files the patterns file lists but the selector routes to a
        // target, so they must NOT be dropped. Each must match a real file (catch typos), and workflow
        // inputs must remain covered so their behavioral tests run in a mixed PR.
        Assert.NotNull(s_map.Prefilter);
        var keepRouted = s_map.Prefilter!.KeepRouted;

        var dead = keepRouted.Where(g => !GlobMatchesAnyTrackedFile(g)).Order(StringComparer.Ordinal).ToList();
        Assert.True(dead.Count == 0, $"prefilter.keep_routed globs matching no tracked file: {string.Join(", ", dead)}");

        Assert.Equal(
            [
                ".github/workflows/auto-rerun-transient-ci-failures.js",
                "eng/github-ci/ci-skip-entirely-patterns.txt"
            ],
            keepRouted.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryTestsSharedFileIsCsAttributedOrRoutedToAllOrExcluded()
    {
        // tests/Shared has both link-compiled *.cs (Layer 1 attributes them precisely, so the run-all
        // fallback treats them as owned — they need no curated rule) and non-source build/fixture infra
        // (props/targets/packages/Docker/Playwright/certs -> ALL). A tests/Shared file that is NEITHER a
        // *.cs NOR matched by an ALL path rule NOR a doc (excluded by the prefilter) would silently select
        // no targeted work: it is not under a project dir (so directory containment can't attribute a
        // loose file there) and would force the run-all fallback. Pin the invariant so a new file type
        // added under tests/Shared can't quietly expand to ALL. (.md files are dropped by the prefilter's
        // **.md.)
        var allGlobs = s_map.PathRules
            .Where(r => r.Targets.Contains("ALL", StringComparer.Ordinal))
            .SelectMany(r => r.Paths)
            .ToList();

        bool RoutedToAll(string file) => allGlobs.Any(g => TestTriggerMap.GlobMatches(g, file));

        var orphaned = s_trackedFiles
            .Where(f => f.StartsWith("tests/Shared/", StringComparison.Ordinal))
            .Where(f => !f.EndsWith(".cs", StringComparison.Ordinal))
            .Where(f => !f.EndsWith(".md", StringComparison.Ordinal))
            .Where(f => !RoutedToAll(f))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(orphaned.Count == 0,
            $"tests/Shared files that are neither *.cs (Layer 1-attributed), *.md (prefiltered), nor routed " +
            $"to ALL (they would silently select nothing): {string.Join(", ", orphaned)}");
    }

    [Theory]
    [InlineData(".github/actions/check-changed-files/action.yml", false, new[] { "Infrastructure.Tests" }, new string[] { })]
    [InlineData(".github/actions/select-tests/action.yml", false, new[] { "Infrastructure.Tests" }, new string[] { })]
    [InlineData(".github/actions/setup-deno/action.yml", false, new[] { "Aspire.Hosting.JavaScript.Tests" }, new[] { "job:extension-e2e" })]
    [InlineData(".github/actions/unlock-macos-keychain/action.yml", true, new string[] { }, new string[] { })]
    public void LocalActionRoutingMatchesItsRegularPrCiConsumers(
        string path,
        bool selectsAll,
        string[] expectedTestProjects,
        string[] expectedJobs)
    {
        var result = SelectWithRealMap(path);

        Assert.Equal(selectsAll, result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);

        if (!selectsAll)
        {
            Assert.Equal(expectedTestProjects.Order(StringComparer.Ordinal), result.TestProjects.Order(StringComparer.Ordinal));
            Assert.Equal(expectedJobs.Order(StringComparer.Ordinal), result.Jobs.Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void EnumerateTestsActionSelectsManagedTestProjectsButNotNonDotnetJobs()
    {
        var result = SelectWithRealMap(".github/actions/enumerate-tests/action.yml");
        var expectedTestProjects = LoadSolutionProjectPaths()
            .Where(projectPath => projectPath.StartsWith("tests/", StringComparison.Ordinal))
            .Select(projectPath => Path.GetFileNameWithoutExtension(projectPath)!)
            .Where(name => name.EndsWith(".Tests", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.False(result.SelectsAll);
        Assert.Empty(result.UnmatchedFiles);
        Assert.Equal(expectedTestProjects, result.TestProjects.Order(StringComparer.Ordinal));
        Assert.Empty(result.Jobs);
    }

    private static SelectionResult SelectWithRealMap(string path, params string[] layer1Affected)
    {
        var projectPaths = LoadSolutionProjectPaths();
        var testProjects = projectPaths
            .Where(projectPath => projectPath.StartsWith("tests/", StringComparison.Ordinal))
            .Select(projectPath => Path.GetFileNameWithoutExtension(projectPath)!)
            .Where(name => name.EndsWith(".Tests", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var testProjectNames = Directory.EnumerateFiles(
                Path.Combine(RepoRoot.Path, "tests"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
        var affectedTestProjectNames = layer1Affected
            .Where(testProjectNames.Contains)
            .ToHashSet(StringComparer.Ordinal);
        var projectDirectories = projectPaths
            .Select(projectPath => Path.GetDirectoryName(projectPath)!.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
        var mapPath = Path.Combine(RepoRoot.Path, "eng", "github-ci", "test-trigger-map.yml");
        var selector = new TestSelector(mapPath, testProjects, projectDirectories, affectedTestProjectNames);

        return selector.Select([path], layer1Affected, new SelectorOptions());
    }

    private static IEnumerable<string> LoadArtifactConsumingTestProjects()
    {
        GraphAffectedProjects.EnsureMSBuildRegistered();

        var solutionPath = Path.Combine(RepoRoot.Path, "Aspire.slnx");
        return LoadArtifactConsumingTestProjectsCore(solutionPath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IEnumerable<string> LoadArtifactConsumingTestProjectsCore(string solutionPath)
    {
        return LoadArtifactConsumingTestProjectMetadata(solutionPath)
            .Where(project => !project.SkipTests)
            .Select(project => project.Name);
    }

    private static IEnumerable<ArtifactConsumingTestProject> LoadArtifactConsumingTestProjectMetadata()
    {
        GraphAffectedProjects.EnsureMSBuildRegistered();

        var solutionPath = Path.Combine(RepoRoot.Path, "Aspire.slnx");
        return LoadArtifactConsumingTestProjectMetadata(solutionPath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IEnumerable<ArtifactConsumingTestProject> LoadArtifactConsumingTestProjectMetadata(string solutionPath)
    {
        var graph = GraphAffectedProjects.BuildGraph(
            RepoRoot.Path,
            solutionPath,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["IsGitHubActionsRunner"] = "true",
                ["IsGithubPullRequest"] = "true",
                ["RunOuterloopTests"] = "false",
                ["RunQuarantinedTests"] = "false",
            });

        return graph.ProjectNodes
            .Where(node => IsMatrixTestProject(node.ProjectInstance.FullPath))
            .Where(node =>
                IsTrue(node.ProjectInstance.GetPropertyValue("RequiresNugets"))
                || IsTrue(node.ProjectInstance.GetPropertyValue("RequiresCliArchive")))
            .Select(node => new ArtifactConsumingTestProject(
                Path.GetFileNameWithoutExtension(node.ProjectInstance.FullPath)!,
                IsTrue(node.ProjectInstance.GetPropertyValue("SkipTests"))))
            .GroupBy(project => project.Name, StringComparer.Ordinal)
            .Select(group => group.First());
    }

    private sealed record ArtifactConsumingTestProject(string Name, bool SkipTests);

    private static bool IsMatrixTestProject(string projectPath)
    {
        var relativePath = Path.GetRelativePath(RepoRoot.Path, projectPath).Replace('\\', '/');
        return relativePath.StartsWith("tests/", StringComparison.Ordinal)
            && Path.GetFileNameWithoutExtension(relativePath).EndsWith(".Tests", StringComparison.Ordinal);
    }

    private static bool IsTrue(string value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

}
