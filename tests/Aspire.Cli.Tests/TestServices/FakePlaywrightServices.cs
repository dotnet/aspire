// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.AspireSkills;
using Aspire.Cli.Agents.Playwright;
using Aspire.Cli.Npm;
using Semver;

namespace Aspire.Cli.Tests.TestServices;

/// <summary>
/// A fake implementation of <see cref="INpmRunner"/> for testing.
/// </summary>
internal sealed class FakeNpmRunner : INpmRunner
{
    public bool IsAvailable { get; set; } = true;
    public Func<string, string, CancellationToken, Task<NpmPackageInfo?>>? ResolvePackageAsyncCallback { get; set; }
    public NpmPackageInfo? ResolveResult { get; set; }
    public bool PackSucceeds { get; set; } = true;
    public byte[] TarballContent { get; set; } = [1, 2, 3];
    public bool InstallGlobalResult { get; set; } = true;
    public int ResolveCallCount { get; private set; }
    public int PackCallCount { get; private set; }
    public int InstallGlobalCallCount { get; private set; }
    public string? ResolvedPackageName { get; private set; }
    public string? ResolvedVersionRange { get; private set; }
    public string? PackOutputDirectory { get; private set; }
    public string? InstalledTarballPath { get; private set; }
    public Action<CancellationToken>? OnResolvePackage { get; set; }

    public Task<NpmPackageInfo?> ResolvePackageAsync(string packageName, string versionRange, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ResolveCallCount++;
        ResolvedPackageName = packageName;
        ResolvedVersionRange = versionRange;
        OnResolvePackage?.Invoke(cancellationToken);

        return ResolvePackageAsyncCallback?.Invoke(packageName, versionRange, cancellationToken) ?? Task.FromResult(ResolveResult);
    }

    public async Task<string?> PackAsync(string packageName, string version, string outputDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PackCallCount++;
        PackOutputDirectory = outputDirectory;
        if (!PackSucceeds)
        {
            return null;
        }

        var tarballPath = Path.Combine(outputDirectory, "package.tgz");
        await File.WriteAllBytesAsync(tarballPath, TarballContent, cancellationToken);

        return tarballPath;
    }

    public Task<bool> InstallGlobalAsync(string tarballPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InstallGlobalCallCount++;
        InstalledTarballPath = tarballPath;

        return Task.FromResult(InstallGlobalResult);
    }
}

/// <summary>
/// A fake implementation of <see cref="INpmProvenanceChecker"/> for testing.
/// </summary>
internal sealed class FakeNpmProvenanceChecker : INpmProvenanceChecker
{
    public ProvenanceVerificationOutcome ProvenanceOutcome { get; set; } = ProvenanceVerificationOutcome.Verified;
    public int CallCount { get; private set; }
    public Func<WorkflowRefInfo, bool>? CapturedValidateWorkflowRef { get; private set; }
    public string? CapturedSriIntegrity { get; private set; }

    public Task<ProvenanceVerificationResult> VerifyProvenanceAsync(string packageName, string version, string expectedSourceRepository, string expectedWorkflowPath, string expectedBuildType, Func<WorkflowRefInfo, bool>? validateWorkflowRef, string? sriIntegrity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        CapturedValidateWorkflowRef = validateWorkflowRef;
        CapturedSriIntegrity = sriIntegrity;

        return Task.FromResult(new ProvenanceVerificationResult
        {
            Outcome = ProvenanceOutcome,
            Provenance = new NpmProvenanceData { SourceRepository = expectedSourceRepository }
        });
    }
}

/// <summary>
/// A fake implementation of <see cref="IAspireSkillsInstaller"/> for testing.
/// </summary>
internal sealed class FakeAspireSkillsInstaller : IAspireSkillsInstaller
{
    internal const string AspireInitSkillName = "aspire-init";
    internal const string AspireMonitoringSkillName = "aspire-monitoring";
    internal const string AspireOrchestrationSkillName = "aspire-orchestration";
    internal const string AspireProjectV2MigrationSkillName = "aspire-project-v2-migration";

    private readonly DirectoryInfo _bundleDirectory;
    private readonly AspireSkillsInstallResult? _result;

    public FakeAspireSkillsInstaller(CliExecutionContext executionContext)
        : this(executionContext, result: null)
    {
    }

    public FakeAspireSkillsInstaller(CliExecutionContext executionContext, AspireSkillsInstallResult? result)
    {
        _bundleDirectory = new DirectoryInfo(Path.Combine(executionContext.WorkingDirectory.FullName, ".fake-aspire-skills-bundle"));
        _result = result;
    }

    public async Task<AspireSkillsInstallResult> InstallAsync(CancellationToken cancellationToken)
    {
        if (_result is not null)
        {
            return _result;
        }

        await EnsureBundleAsync(cancellationToken);
        var bundle = await new AspireSkillsBundleProvider().LoadAsync(_bundleDirectory, cancellationToken);
        return AspireSkillsInstallResult.Installed(bundle);
    }

    private async Task EnsureBundleAsync(CancellationToken cancellationToken)
    {
        if (_bundleDirectory.Exists)
        {
            return;
        }

        var files = new Dictionary<(string SkillName, string RelativePath), string>
        {
            [(CommonAgentApplicators.AspireSkillName, "SKILL.md")] =
                """
                ---
                name: aspire
                description: "Aspire CLI commands and workflows for distributed apps"
                ---

                # Aspire Skill
                """,
            [(CommonAgentApplicators.AspireSkillName, Path.Combine("references", "app-commands.md"))] = "# App commands",
            [(CommonAgentApplicators.AspireSkillName, Path.Combine("evals", "evals.json"))] = "{}",
            [(CommonAgentApplicators.AspireifySkillName, "SKILL.md")] =
                """
                ---
                name: aspireify
                description: "One-time setup: wire up AppHost with discovered projects"
                ---

                # Aspireify
                """,
            [(CommonAgentApplicators.AspireDeploymentSkillName, "SKILL.md")] =
                """
                ---
                name: aspire-deployment
                description: "Aspire deployment target selection, preflight, publish, and deploy workflows"
                ---

                # Aspire Deployment
                """,
            [(CommonAgentApplicators.AspireDeploymentSkillName, Path.Combine("references", "preflight.md"))] = "# Preflight",
            [(AspireInitSkillName, "SKILL.md")] =
                """
                ---
                name: aspire-init
                description: "First-run flow for adding Aspire to a repo"
                ---

                # Aspire Init
                """,
            [(AspireMonitoringSkillName, "SKILL.md")] =
                """
                ---
                name: aspire-monitoring
                description: "Observe Aspire apps with logs, traces, metrics, and resource state"
                ---

                # Aspire Monitoring
                """,
            [(AspireOrchestrationSkillName, "SKILL.md")] =
                """
                ---
                name: aspire-orchestration
                description: "Manage Aspire AppHost lifecycle and resource commands"
                ---

                # Aspire Orchestration
                """,
            [(AspireProjectV2MigrationSkillName, "SKILL.md")] =
                """
                ---
                name: aspire-project-v2-migration
                description: "Migrate approved project resources in Aspire 13.6 or newer AppHosts"
                ---

                # Project v2 migration
                """,
            [(AspireProjectV2MigrationSkillName, Path.Combine("references", "migration-patterns.md"))] = "# Migration patterns",
            [(AspireProjectV2MigrationSkillName, Path.Combine("references", "compatibility-and-validation.md"))] = "# Compatibility and validation",
            [(AspireProjectV2MigrationSkillName, Path.Combine("evals", "eval.yaml"))] = "stimuli: []"
        };

        foreach (var ((skillName, relativePath), content) in files)
        {
            var path = Path.Combine(_bundleDirectory.FullName, "skills", skillName, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content, cancellationToken);
        }

        var manifest = new SkillBundleManifest
        {
            Version = AspireSkillsInstaller.Version,
            Supports = new SkillBundleSupports
            {
                AspireCli = ">=0.0.0 <999.0.0",
                AspireSdk = ">=0.0.0 <999.0.0"
            },
            Skills =
            [
                CreateSkill(CommonAgentApplicators.AspireSkillName, ["evals"], files),
                CreateSkill(CommonAgentApplicators.AspireifySkillName, ["evals"], files),
                CreateSkill(CommonAgentApplicators.AspireDeploymentSkillName, ["evals"], files),
                CreateSkill(AspireInitSkillName, ["evals"], files),
                CreateSkill(AspireMonitoringSkillName, ["evals"], files),
                CreateSkill(AspireOrchestrationSkillName, ["evals"], files),
                CreateSkill(AspireProjectV2MigrationSkillName, ["evals"], files)
            ]
        };

        var manifestJson = JsonSerializer.Serialize(manifest, AspireSkillsJsonSerializerContext.Default.SkillBundleManifest);
        await File.WriteAllTextAsync(Path.Combine(_bundleDirectory.FullName, "skill-manifest.json"), manifestJson, cancellationToken);
    }

    private SkillBundleSkill CreateSkill(string skillName, string[] installExcludedRelativePaths, Dictionary<(string SkillName, string RelativePath), string> files)
    {
        return new SkillBundleSkill
        {
            Name = skillName,
            Description = $"{skillName} skill",
            InstallExcludedRelativePaths = installExcludedRelativePaths,
            Files = files
                .Where(entry => string.Equals(entry.Key.SkillName, skillName, StringComparison.Ordinal))
                .Select(entry => new SkillBundleFile
                {
                    RelativePath = entry.Key.RelativePath,
                    Sha512 = ComputeSha512(Path.Combine(_bundleDirectory.FullName, "skills", skillName, entry.Key.RelativePath))
                })
                .ToArray()
        };
    }

    private static string ComputeSha512(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA512.HashData(stream)).ToLowerInvariant();
    }
}

/// <summary>
/// A fake implementation of <see cref="IPlaywrightCliRunner"/> for testing.
/// </summary>
internal sealed class FakePlaywrightCliRunner : IPlaywrightCliRunner
{
    public SemVersion? InstalledVersion { get; set; }
    public bool InstallSkillsResult { get; set; } = true;
    public int GetVersionCallCount { get; private set; }
    public int InstallSkillsCallCount { get; private set; }
    public string? InstallSkillsWorkingDirectory { get; private set; }
    public Action<string>? OnInstallSkills { get; set; }
    public Dictionary<string, byte[]> SkillFiles { get; } = new(StringComparer.Ordinal)
    {
        ["SKILL.md"] = "# Playwright CLI\n\nSee [commands](references/commands.md)."u8.ToArray(),
        [Path.Combine("references", "commands.md")] = "# Playwright CLI commands"u8.ToArray()
    };

    public Task<SemVersion?> GetVersionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetVersionCallCount++;

        return Task.FromResult(InstalledVersion);
    }

    public async Task<bool> InstallSkillsAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InstallSkillsCallCount++;
        InstallSkillsWorkingDirectory = workingDirectory;
        OnInstallSkills?.Invoke(workingDirectory);

        if (InstallSkillsResult)
        {
            foreach (var (relativePath, content) in SkillFiles)
            {
                var path = Path.Combine(
                    workingDirectory, PlaywrightCliInstaller.s_primarySkillBaseDirectory, PlaywrightCliInstaller.PlaywrightCliSkillName, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllBytesAsync(path, content, cancellationToken);
            }
        }

        return InstallSkillsResult;
    }
}
