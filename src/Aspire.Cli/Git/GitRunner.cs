// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text;
using Aspire.Cli.DotNet;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Git;

internal sealed class GitRunner(
    CliExecutionContext executionContext,
    ICliHostEnvironment hostEnvironment,
    IProcessExecutionFactory processExecutionFactory) : IGitRunner
{
    public async Task CloneAsync(string repository, DirectoryInfo destination, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new StringBuilder();
        try
        {
            // Independent object files let callers clear read-only attributes during cleanup
            // without changing a local source repository through Git's default hard links.
            await using var process = processExecutionFactory.CreateExecution(
                "git", ["clone", "--depth", "1", "--no-hardlinks", "--", repository, destination.FullName],
                hostEnvironment.SupportsInteractiveInput ? null : new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" },
                executionContext.WorkingDirectory,
                new ProcessInvocationOptions { StandardErrorCallback = line => errors.AppendLine(line) });
            if (!await process.StartAsync(cancellationToken))
            {
                throw new GitCloneException("Git could not be started.", null, null);
            }

            var exitCode = await process.WaitForExitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (exitCode != 0)
            {
                throw new GitCloneException(errors.ToString().Trim(), exitCode, null);
            }
        }
        catch (Win32Exception ex)
        {
            throw new GitCloneException(ex.Message, null, ex);
        }
    }
}
