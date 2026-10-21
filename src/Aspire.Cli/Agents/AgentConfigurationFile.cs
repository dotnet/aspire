// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Agents;

internal static class AgentConfigurationFile
{
    public static async Task WriteAsync(
        string path, JsonObject settings, AgentConfigurationReadContext reads, CancellationToken cancellationToken)
    {
        path = Path.GetFullPath(path);
        RejectSymbolicLink(path);
        // All agent settings writers use the same sidecar, including MCP and user hooks.
        // Acquire only after read-only configuration validation, so refused setup creates no files.
        FileLock fileLock;
        try
        {
            fileLock = await FileLock.AcquireAsync(
                Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.aspire.lock"),
                cancellationToken, TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException ex)
        {
            throw new IOException(ex.Message, ex);
        }
        using var lease = fileLock;
        await reads.ValidateAsync(path, cancellationToken);
        RejectSymbolicLink(path);

        var existed = File.Exists(path);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        var created = false;
        try
        {
            await using (var stream = CreateStagingFile(temporary, path, existed))
            {
                created = true;
                if (existed && !OperatingSystem.IsWindows())
                {
                    await PreserveUnixMetadataAsync(path, temporary, cancellationToken);
                    stream.SetLength(0);
                }

                await JsonSerializer.SerializeAsync(stream, settings, JsonSourceGenerationContext.Default.JsonObject, cancellationToken);
            }

            // Recheck file bytes, missing inputs and policy-fragment membership after staging.
            await reads.ValidateAsync(path, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            RejectSymbolicLink(path);
            if (existed && OperatingSystem.IsWindows())
            {
                // ReplaceFile preserves the destination DACL, including inheritance flags.
                File.Replace(temporary, path, destinationBackupFileName: null);
            }
            else
            {
                // Same-directory rename is atomic. For a new target, never overwrite a
                // file created after validation. Existing hard-link aliases retain their old bytes.
                File.Move(temporary, path, overwrite: existed);
            }
        }
        finally
        {
            if (created)
            {
                File.Delete(temporary);
            }
        }
    }

    private static FileStream CreateStagingFile(string temporary, string destination, bool existed)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsStagingFile(temporary, destination, existed);
        }

        return new FileStream(temporary, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
        });
    }

    [SupportedOSPlatform("windows")]
    private static FileStream CreateWindowsStagingFile(string temporary, string destination, bool existed)
    {
        var security = existed
            ? new FileInfo(destination).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group)
            : new FileSecurity();
        if (!existed)
        {
            using var identity = WindowsIdentity.GetCurrent();
            var owner = identity.User ?? throw new UnauthorizedAccessException();
            security.SetOwner(owner);
            security.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl, AccessControlType.Allow));
        }

        // Freeze the original effective ACL before creating any credential-bearing copy.
        // Parent inheritance must not grant readers access while validation awaits.
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: true);
        return new FileInfo(temporary).Create(FileMode.CreateNew, FileSystemRights.Read | FileSystemRights.Write,
            FileShare.None, 4096, FileOptions.Asynchronous, security);
    }

    [UnsupportedOSPlatform("windows")]
    private static async Task PreserveUnixMetadataAsync(string source, string destination, CancellationToken cancellationToken)
    {
        // chmod alone drops extended ACLs and can grant access through an ACL mask.
        // Both GNU and macOS cp -p preserve access ACLs. Check ownership separately:
        // macOS can report success even when the caller cannot preserve uid/gid.
        // https://www.gnu.org/software/coreutils/manual/html_node/cp-invocation.html
        // https://www.manpagez.com/man/1/cp/
        await RunMetadataToolAsync("/bin/cp", ["-p", source, destination], source, cancellationToken);
        var owners = await RunMetadataToolAsync("/usr/bin/stat",
            [OperatingSystem.IsMacOS() ? "-f" : "-c", "%u:%g", source, destination], source, cancellationToken);
        var values = owners.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length != 2 || values[0] != values[1] || File.GetUnixFileMode(source) != File.GetUnixFileMode(destination))
        {
            throw Refused(source);
        }
    }

    private static async Task<string> RunMetadataToolAsync(
        string executable, string[] arguments, string source, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        var result = await ProcessCaptureRunner.RunAsync(start, TimeSpan.FromSeconds(10),
            (process, token) => Task.WhenAll(process.StandardOutput.ReadToEndAsync(token), process.StandardError.ReadToEndAsync(token)),
            () => [], NullLogger.Instance, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.FailureKind is not null || result.ExitCode != 0 ||
            result.Capture.Length != 2 || !string.IsNullOrWhiteSpace(result.Capture[1]))
        {
            throw Refused(source);
        }

        return result.Capture[0];
    }

    private static void RejectSymbolicLink(string path)
    {
        if (new FileInfo(path).LinkTarget is not null)
        {
            throw Refused(path);
        }
    }

    private static IOException Refused(string path)
        => new(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.ConfigurationWriteRefused, path));
}
