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
    public async Task<string> ResolveReferenceAsync(string repository, string? reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (reference is not null && IsCommitSha(reference))
        {
            return reference.ToLowerInvariant();
        }

        var requestedRef = reference ?? "HEAD";
        string[] candidates = requestedRef == "HEAD" || requestedRef.StartsWith("refs/", StringComparison.Ordinal)
            ? [$"{requestedRef}^{{}}", requestedRef]
            : [$"refs/heads/{requestedRef}", $"refs/tags/{requestedRef}^{{}}", $"refs/tags/{requestedRef}"];
        var output = await RunAsync(["ls-remote", "--", repository, .. candidates], executionContext.WorkingDirectory, cancellationToken);

        // git ls-remote emits "<object SHA>\t<ref>", including a peeled commit for annotated tags:
        //   abc...123\trefs/tags/v1
        //   def...456\trefs/tags/v1^{}
        // Match exact names, not Git's wildcard patterns, and prefer the peeled tag over its tag object.
        var refs = new Dictionary<string, string>(StringComparer.Ordinal);
        using var reader = new StringReader(output);
        while (reader.ReadLine() is { } line)
        {
            var separator = line.IndexOf('\t');
            if (separator > 0 && IsCommitSha(line[..separator]))
            {
                refs[line[(separator + 1)..]] = line[..separator].ToLowerInvariant();
            }
        }

        foreach (var candidate in candidates)
        {
            if (refs.TryGetValue(candidate, out var sha))
            {
                return sha;
            }
        }

        throw new GitReferenceException(requestedRef);
    }

    public async Task CloneAsync(string repository, DirectoryInfo destination, string commitSha, CancellationToken cancellationToken)
    {
        if (!IsCommitSha(commitSha))
        {
            throw new ArgumentException("A full commit SHA is required.", nameof(commitSha));
        }

        if (!Path.IsPathRooted(repository) && !repository.Contains(':'))
        {
            var localPath = Path.GetFullPath(repository, executionContext.WorkingDirectory.FullName);
            if (Directory.Exists(localPath))
            {
                // Fetch runs inside the new checkout, not the caller's working directory.
                repository = localPath;
            }
        }

        // Fetch the resolved SHA, not the original branch/tag: it can move between resolution
        // and checkout. A depth-one fetch also avoids local clone hard links, so cleanup cannot
        // change source repository object attributes. https://git-scm.com/docs/git-fetch
        await RunAsync(["init", "--quiet", $"--object-format={(commitSha.Length == 64 ? "sha256" : "sha1")}", "--", destination.FullName],
            executionContext.WorkingDirectory, cancellationToken);
        await RunAsync(["fetch", "--depth", "1", "--no-tags", "--", repository, commitSha], destination, cancellationToken);
        await RunAsync(["checkout", "--detach", "--force", "FETCH_HEAD"], destination, cancellationToken);
        var actualSha = (await RunAsync(["rev-parse", "--verify", "HEAD"], destination, cancellationToken)).Trim();
        if (!string.Equals(actualSha, commitSha, StringComparison.OrdinalIgnoreCase))
        {
            throw new GitReferenceException(commitSha);
        }
    }

    private static bool IsCommitSha(string value) =>
        value.Length is 40 or 64 && value.All(char.IsAsciiHexDigit);

    private async Task<string> RunAsync(string[] arguments, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new StringBuilder();
        var errors = new StringBuilder();
        try
        {
            await using var process = processExecutionFactory.CreateExecution(
                "git", arguments,
                hostEnvironment.SupportsInteractiveInput ? null : new Dictionary<string, string> { ["GIT_TERMINAL_PROMPT"] = "0" },
                workingDirectory,
                new ProcessInvocationOptions
                {
                    StandardOutputCallback = line => output.AppendLine(line),
                    StandardErrorCallback = line => errors.AppendLine(line)
                });
            if (!await process.StartAsync(cancellationToken))
            {
                throw new GitCommandException("Git could not be started.", null, null);
            }

            var exitCode = await process.WaitForExitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (exitCode != 0)
            {
                throw new GitCommandException(errors.ToString().Trim(), exitCode, null);
            }

            return output.ToString();
        }
        catch (Win32Exception ex)
        {
            throw new GitCommandException(ex.Message, null, ex);
        }
    }
}
