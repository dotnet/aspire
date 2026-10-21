// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

public class WindowsStorageBenchmarkTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RefusesProvisioningOutsideBenchmarkRunner()
    {
        var script = Path.Combine(RepoRoot.Path, "eng", "scripts", "prepare-windows-storage-benchmark.ps1");
        using var command = new PowerShellCommand(script, output)
            .WithEnvironmentVariable("GITHUB_ACTIONS", "false")
            .WithTimeout(TimeSpan.FromMinutes(1));
        var result = await command.ExecuteAsync("-Mode", "ntfs", "-Suffix", "-ntfs-1");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Storage provisioning is restricted", result.Output);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RefusesArtifactsFromAnUnsuccessfulRun()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var script = Path.Combine(RepoRoot.Path, "eng", "scripts", "install-windows-storage-benchmark-cli.ps1");
        var fixture = Path.Combine(workspace.Path, "reject-artifact.ps1");
        File.WriteAllText(fixture, """
            param([string]$Script)
            function gh {
                if ($args[0] -ne 'api') { throw 'Unexpected artifact download.' }
                '{"conclusion":"failure","path":".github/workflows/build-cli-native-archives.yml"}'
            }
            & $Script -RunId 42 -Mode ntfs -Sample 1
            """);
        using var command = new PowerShellCommand(fixture, output)
            .WithEnvironmentVariable("GITHUB_REPOSITORY", "radical/aspire")
            .WithEnvironmentVariable("GITHUB_EVENT_NAME", "workflow_dispatch")
            .WithTimeout(TimeSpan.FromMinutes(1));
        var result = await command.ExecuteAsync("-Script", $"\"{script}\"");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Consumer validation requires a successful", result.Output);
    }

    [Theory]
    [InlineData("self-hosted")]
    [InlineData("")]
    [RequiresTools(["pwsh"])]
    public async Task RefusesNonHostedRunners(string runnerEnvironment)
    {
        var script = Path.Combine(RepoRoot.Path, "eng", "scripts", "prepare-windows-storage-benchmark.ps1");
        using var command = new PowerShellCommand(script, output)
            .WithEnvironmentVariable("GITHUB_ACTIONS", "true")
            .WithEnvironmentVariable("GITHUB_REPOSITORY", "radical/aspire")
            .WithEnvironmentVariable("GITHUB_EVENT_NAME", "workflow_dispatch")
            .WithEnvironmentVariable("RUNNER_ARCH", "ARM64")
            .WithEnvironmentVariable("OS", "Windows_NT")
            .WithTimeout(TimeSpan.FromMinutes(1));
        var result = await command.ExecuteAsync("-RunnerEnvironment", $"\"{runnerEnvironment}\"");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Storage provisioning is restricted", result.Output);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PreservesEvidenceWhenInventoryFails()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var script = Path.Combine(RepoRoot.Path, "eng", "scripts", "prepare-windows-storage-benchmark.ps1");
        var fixture = Path.Combine(workspace.Path, "fail-inventory.ps1");
        File.WriteAllText(fixture, """
            param([string]$Script)
            function Get-Disk { throw 'Injected disk inventory failure.' }
            & $Script -Mode baseline -Suffix '-baseline-1' -RunnerEnvironment github-hosted
            """);
        using var command = new PowerShellCommand(fixture, output)
            .WithEnvironmentVariable("GITHUB_ACTIONS", "true")
            .WithEnvironmentVariable("GITHUB_REPOSITORY", "radical/aspire")
            .WithEnvironmentVariable("GITHUB_EVENT_NAME", "workflow_dispatch")
            .WithEnvironmentVariable("RUNNER_ARCH", "ARM64")
            .WithEnvironmentVariable("OS", "Windows_NT")
            .WithEnvironmentVariable("RUNNER_TEMP", workspace.Path)
            .WithTimeout(TimeSpan.FromMinutes(1));
        var result = await command.ExecuteAsync("-Script", $"\"{script}\"");

        Assert.NotEqual(0, result.ExitCode);
        using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace.Path, "windows-storage-benchmark", "storage.json")));
        Assert.Equal("not-exercised", report.RootElement.GetProperty("status").GetString());
        Assert.Equal("disk inventory", report.RootElement.GetProperty("stage").GetString());
        Assert.Equal("Injected disk inventory failure.", report.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RequiresRequestedVolumeAndPositiveDevDriveIdentification()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var script = Path.Combine(RepoRoot.Path, "eng", "scripts", "prepare-windows-storage-benchmark.ps1");
        var fixture = Path.Combine(workspace.Path, "check-treatment.ps1");
        File.WriteAllText(fixture, """
            param([string]$Script)
            $ErrorActionPreference = 'Stop'
            . $Script
            $ntfs = [pscustomobject]@{ DriveLetter = 'D'; FileSystem = 'NTFS' }
            $refs = [pscustomobject]@{ DriveLetter = 'D'; FileSystem = 'ReFS' }
            Assert-BenchmarkTreatment baseline ([pscustomobject]@{ DriveLetter = 'C'; FileSystem = 'NTFS' })
            Assert-BenchmarkTreatment ntfs $ntfs 7 7
            Assert-BenchmarkTreatment devdrive $refs 7 7 'This is a trusted developer volume.' 0
            $cases = @(
                @{ Mode = 'baseline'; WorkspaceVolume = $ntfs },
                @{ Mode = 'ntfs'; WorkspaceVolume = $refs; ExpectedDisk = 7; ActualDisk = 7 },
                @{ Mode = 'ntfs'; WorkspaceVolume = $ntfs; ExpectedDisk = 7; ActualDisk = 8 },
                @{ Mode = 'devdrive'; WorkspaceVolume = $refs; ExpectedDisk = 7; ActualDisk = 7; DevDriveQuery = 'This is not a developer volume.'; DevDriveQueryExitCode = 0 },
                @{ Mode = 'devdrive'; WorkspaceVolume = $refs; ExpectedDisk = 7; ActualDisk = 7; DevDriveQuery = 'This is a trusted developer volume.'; DevDriveQueryExitCode = 1 }
            )
            foreach ($case in $cases) {
                $rejected = $false
                try { Assert-BenchmarkTreatment @case } catch { $rejected = $true }
                if (-not $rejected) { throw 'An invalid storage treatment was accepted.' }
            }
            Write-Output 'Rejected all 5 invalid treatments.'
            """);
        using var command = new PowerShellCommand(fixture, output).WithTimeout(TimeSpan.FromMinutes(1));
        var result = await command.ExecuteAsync("-Script", $"\"{script}\"");
        result.EnsureSuccessful();
        Assert.Contains("Rejected all 5 invalid treatments", result.Output);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RejectsUnsafeDiskInventories()
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var script = Path.Combine(RepoRoot.Path, "eng", "scripts", "prepare-windows-storage-benchmark.ps1");
        var fixture = Path.Combine(workspace.Path, "check.ps1");
        File.WriteAllText(fixture, """
            param([string]$Script)
            $ErrorActionPreference = 'Stop'
            . $Script
            function Disk($number, $style = 'RAW', $bus = 'NVMe', $boot = $false, $system = $false, $size = 220GB) {
                return [pscustomobject]@{ Number = $number; PartitionStyle = $style; BusType = $bus; IsBoot = $boot; IsSystem = $system; Size = $size }
            }
            $safe = Disk 7
            $accepted = Get-BenchmarkDiskDecision @($safe, (Disk 1 'GPT' 'SAS' $true $true)) $false $false $false
            if (-not $accepted.Eligible -or $accepted.DiskNumber -ne 7) { throw 'Did not select the uniquely eligible disk.' }
            $cases = @(
                @{ Disks = @($safe); DriveOccupied = $true; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @($safe); DriveOccupied = $false; WorkspacePopulated = $true; WorkspaceIsLink = $false },
                @{ Disks = @($safe); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $true },
                @{ Disks = @(); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @($safe, (Disk 8)); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @((Disk 0 'GPT')); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @((Disk 0 'RAW' 'SAS')); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @((Disk 0 'RAW' 'NVMe' $true)); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @((Disk 0 'RAW' 'NVMe' $false $true)); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false },
                @{ Disks = @((Disk 0 'RAW' 'NVMe' $false $false 20GB)); DriveOccupied = $false; WorkspacePopulated = $false; WorkspaceIsLink = $false }
            )
            foreach ($case in $cases) {
                $decision = Get-BenchmarkDiskDecision @case
                if ($decision.Eligible -or [string]::IsNullOrWhiteSpace($decision.Reason)) { throw 'Unsafe inventory accepted or rejection unexplained.' }
            }
            Write-Output 'Accepted the unused disk and rejected all 10 unsafe inventories.'
            """);
        using var command = new PowerShellCommand(fixture, output).WithTimeout(TimeSpan.FromMinutes(1));
        var result = await command.ExecuteAsync("-Script", $"\"{script}\"");
        result.EnsureSuccessful();
        Assert.Contains("rejected all 10 unsafe inventories", result.Output);
    }
}
