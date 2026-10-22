// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Aspire.Cli.Agents;
using Aspire.Cli.Tests.TestServices;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Agents;

public class AgentConfigurationPublicationTests(ITestOutputHelper output) : IDisposable
{
    private readonly AgentConfigurationTestContext _context = new(output);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeEdit_RefusesAChangedOrNewDestination(bool existed)
    {
        var path = _context.Copilot.GetIntegrationConfigPath(AgentConfigurationScope.Project, _context.Project);
        if (existed)
        {
            await AgentConfigurationTestContext.WriteAsync(path, "{}");
        }
        const string changed = """{"userEdit":true}""";
        var client = new InterceptingAgentClient(_context.Copilot, () => AgentConfigurationTestContext.WriteAsync(path, changed));

        var result = Assert.Single(await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [client]).DefaultTimeout());

        Assert.Equal(AgentConfigurationStatus.Failed, result.Status);
        Assert.Equal(changed, await File.ReadAllTextAsync(path));
        Assert.Equal([path], Directory.GetFiles(_context.Project.FullName, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("copilot")]
    [InlineData("claude")]
    [InlineData("opencode")]
    public async Task NativeEdit_RefusesChangedInheritedSettings(string clientId)
    {
        var original = _context.Clients.Single(client => client.Id == clientId);
        var inherited = original.GetIntegrationConfigPath(AgentConfigurationScope.Global, _context.Project);
        await AgentConfigurationTestContext.WriteAsync(inherited, "{}");
        var client = new InterceptingAgentClient(original,
            () => AgentConfigurationTestContext.WriteAsync(inherited, """{"userEdit":true}"""));

        var result = Assert.Single(await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [client]).DefaultTimeout());

        Assert.Equal(AgentConfigurationStatus.Failed, result.Status);
        Assert.False(File.Exists(result.TargetPath));
    }

    [Theory]
    [InlineData("directory")]
    [InlineData("added")]
    [InlineData("removed")]
    [InlineData("modified")]
    public async Task ClaudeEdit_RevalidatesManagedFragmentAbsenceMembershipAndContent(string change)
    {
        var directory = Path.Combine(_context.ClaudeManagedDirectory, "managed-settings.d");
        var fragment = Path.Combine(directory, "team.json");
        if (change != "directory")
        {
            Directory.CreateDirectory(directory);
        }
        if (change is "removed" or "modified")
        {
            await File.WriteAllTextAsync(fragment, "{}");
        }
        var client = new InterceptingAgentClient(_context.ClaudeCode, async () =>
        {
            if (change == "directory")
            {
                Directory.CreateDirectory(directory);
            }
            else if (change == "removed")
            {
                File.Delete(fragment);
            }
            else
            {
                await File.WriteAllTextAsync(fragment, """{"blockedMarketplaces":[]}""");
            }
        });

        var result = Assert.Single(await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [client]).DefaultTimeout());

        Assert.Equal(AgentConfigurationStatus.Failed, result.Status);
        Assert.False(File.Exists(result.TargetPath));
        Assert.Empty(Directory.GetFiles(_context.Project.FullName, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ClaudeEdit_IgnoresNonSettingsFragmentChanges()
    {
        var directory = Path.Combine(_context.ClaudeManagedDirectory, "managed-settings.d");
        Directory.CreateDirectory(directory);
        var client = new InterceptingAgentClient(_context.ClaudeCode,
            () => File.WriteAllTextAsync(Path.Combine(directory, ".ignored.json"), "{malformed"));

        var result = Assert.Single(await _context.ConfigureIntegrationAsync(AgentConfigurationScope.Project, [client]).DefaultTimeout());

        Assert.Equal(AgentConfigurationStatus.Configured, result.Status);
    }

    [Fact]
    public async Task NativeAndMcpPublication_RejectsTheStaleCompetingWriter()
    {
        var path = _context.OpenCode.GetIntegrationConfigPath(AgentConfigurationScope.Global, _context.Project);
        await AgentConfigurationTestContext.WriteAsync(path, "{}");
        var nativeReads = new AgentConfigurationReadContext();
        var native = (await AgentConfigurationJson.ReadAsync(path, nativeReads, CancellationToken.None))!;
        await _context.OpenCode.UpdateIntegrationConfigAsync(
            native, _context.Project, AgentConfigurationScope.Global, nativeReads, CancellationToken.None);
        var mcpReads = new AgentConfigurationReadContext();
        var (mcp, mcpPath) = await _context.OpenCode.ReadMcpConfigAsync(
            path, _context.Project, AgentConfigurationScope.Global, mcpReads, CancellationToken.None);
        _context.OpenCode.UpdateMcpConfig(mcp, mcpPath, configureServer: true, AgentConfigurationScope.Global);
        Task[] writes =
        [
            AgentConfigurationFile.WriteAsync(path, native, nativeReads, CancellationToken.None),
            AgentConfigurationFile.WriteAsync(mcpPath, mcp, mcpReads, CancellationToken.None)
        ];
        var errors = await Task.WhenAll(writes.Select(write => Record.ExceptionAsync(() => write).AsTask())).DefaultTimeout();
        Assert.Single(errors, error => error is null);
        Assert.IsType<IOException>(Assert.Single(errors, error => error is not null));

        var published = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        Assert.NotEqual(published["skills"] is not null, published["mcp"] is not null);
        Assert.Equal([path], Directory.GetFiles(Path.GetDirectoryName(path)!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Publication_RefusesExistingAndDanglingSymbolicLinks(bool dangling)
    {
        var path = Path.Combine(_context.Project.FullName, "settings.json");
        var target = Path.Combine(_context.Project.FullName, "linked.json");
        if (!dangling)
        {
            await File.WriteAllTextAsync(target, "{}");
        }
        TestSymlinkHelper.TryCreateSymlink(path, target, isDirectory: false);
        var reads = new AgentConfigurationReadContext();
        await reads.ReadAsync(path, CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() => AgentConfigurationFile.WriteAsync(
            path, new JsonObject(), reads, CancellationToken.None)).DefaultTimeout();

        Assert.Equal(target, new FileInfo(path).LinkTarget);
        Assert.Equal(!dangling, File.Exists(target));
        if (!dangling)
        {
            Assert.Equal("{}", await File.ReadAllTextAsync(target));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WindowsPublication_PreservesPrivateAclUnderAnInheritablePublicParent(bool publicationFails)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows ACL validation.");
            return;
        }
        var path = Path.Combine(_context.Project.FullName, "settings.json");
        var directory = _context.Project.GetAccessControl();
        directory.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Read,
            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
        _context.Project.SetAccessControl(directory);
        await File.WriteAllTextAsync(path, """{"token":"original"}""");
        using var identity = WindowsIdentity.GetCurrent();
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(identity.User!);
        security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
        var before = new FileInfo(path).GetAccessControl();
        var reads = new AgentConfigurationReadContext();
        await reads.ReadAsync(path, CancellationToken.None);
        // An open reader that does not share deletion forces ReplaceFile to fail after staging.
        using var reader = publicationFails ? File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        var write = AgentConfigurationFile.WriteAsync(path, new JsonObject { ["token"] = "replacement" }, reads, CancellationToken.None);
        if (publicationFails)
        {
            await Assert.ThrowsAsync<IOException>(() => write).DefaultTimeout();
        }
        else
        {
            await write.DefaultTimeout();
        }

        Assert.Equal(before.GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group),
            new FileInfo(path).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group));
        Assert.Equal(publicationFails ? "original" : "replacement",
            JsonNode.Parse(await File.ReadAllTextAsync(path))!["token"]!.GetValue<string>());
        Assert.Equal([path], Directory.GetFiles(_context.Project.FullName));
    }

    // Keep the basic rewrite case runnable on BusyBox installations without libacl.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnixPublication_UpdatesExistingPrivateFileWithSystemMetadataTools(bool parentAcl)
    {
        if (OperatingSystem.IsWindows() || parentAcl && !OperatingSystem.IsLinux())
        {
            Assert.Skip("Unix metadata tools validation.");
            return;
        }
        var path = Path.Combine(_context.Project.FullName, "settings.json");
        await File.WriteAllTextAsync(path, """{"token":"original"}""");
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        File.SetUnixFileMode(path, mode);
        string? acl = null;
        if (parentAcl)
        {
            TestPosixAcl.SetDefault(_context.Project.FullName, "user::rwx,user:65534:r-x,group::---,mask::r-x,other::---");
            acl = TestPosixAcl.Get(path);
        }
        var reads = new AgentConfigurationReadContext();
        await reads.ReadAsync(path, CancellationToken.None);

        await AgentConfigurationFile.WriteAsync(path, new JsonObject { ["token"] = "replacement" },
            reads, CancellationToken.None).DefaultTimeout();

        Assert.Equal(mode, File.GetUnixFileMode(path));
        if (parentAcl)
        {
            Assert.Equal(acl, TestPosixAcl.Get(path));
        }
        Assert.Equal("replacement", JsonNode.Parse(await File.ReadAllTextAsync(path))!["token"]!.GetValue<string>());
        Assert.Equal([path], Directory.GetFiles(_context.Project.FullName));
    }

    [Fact]
    public async Task LinuxPublication_PreservesAnExtendedAclRatherThanOnlyItsMode()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("Linux POSIX ACL validation.");
            return;
        }
        var path = Path.Combine(_context.Project.FullName, "settings.json");
        await File.WriteAllTextAsync(path, """{"token":"original"}""");
        // The group mode reports r-- through the mask, but the owning group has no access.
        TestPosixAcl.Set(path, "user::rw-,user:65534:r--,group::---,mask::r--,other::---");
        var before = TestPosixAcl.Get(path);
        var mode = File.GetUnixFileMode(path);
        var reads = new AgentConfigurationReadContext();
        await reads.ReadAsync(path, CancellationToken.None);

        await AgentConfigurationFile.WriteAsync(path, new JsonObject { ["token"] = "replacement" },
            reads, CancellationToken.None).DefaultTimeout();

        Assert.Equal(before, TestPosixAcl.Get(path));
        Assert.Equal(mode, File.GetUnixFileMode(path));
        Assert.Equal("replacement", JsonNode.Parse(await File.ReadAllTextAsync(path))!["token"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnixPublication_RefusesPermissionsChangedDuringStaging(bool accessAcl)
    {
        if (OperatingSystem.IsWindows() || accessAcl && !OperatingSystem.IsLinux())
        {
            Assert.Skip("Unix mode validation; extended ACL validation requires Linux.");
            return;
        }
        var path = Path.Combine(_context.Project.FullName, "settings.json");
        const string original = """{"token":"original"}""";
        await File.WriteAllTextAsync(path, original);
        if (accessAcl)
        {
            TestPosixAcl.Set(path, "user::rw-,user:65534:r--,group::---,mask::r--,other::---");
        }
        else
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
        var mode = File.GetUnixFileMode(path);
        const string restrictedAcl = "user::rw-,user:65534:---,group::---,mask::r--,other::---";
        string? expectedAcl = null;
        var converter = new InterceptingStringJsonConverter(() =>
        {
            if (OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            // Serialization runs after the writer has copied permissions into staging.
            if (accessAcl)
            {
                TestPosixAcl.Set(path, restrictedAcl);
                expectedAcl = TestPosixAcl.Get(path);
            }
            else
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        });
        // Use the writer's options so JsonNode retains this converter during serialization.
        var settings = new JsonObject
        {
            ["token"] = JsonValue.Create("replacement",
                JsonMetadataServices.CreateValueInfo<string>(JsonSourceGenerationContext.Default.Options, converter))
        };
        var reads = new AgentConfigurationReadContext();
        await reads.ReadAsync(path, CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() => AgentConfigurationFile.WriteAsync(
            path, settings, reads, CancellationToken.None)).DefaultTimeout();

        Assert.Equal(original, await File.ReadAllTextAsync(path));
        if (accessAcl)
        {
            Assert.Equal(mode, File.GetUnixFileMode(path));
            Assert.Equal(expectedAcl, TestPosixAcl.Get(path));
        }
        else
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        }
        Assert.Equal([path], Directory.GetFiles(_context.Project.FullName));
    }

    public void Dispose() => _context.Dispose();
}
