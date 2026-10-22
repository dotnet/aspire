// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal interface IConfigSourceGitClient
{
    Task<(string Commit, string Version, JsonObject Configuration)> AcquireAsync(ConfigSource source, string directory, CancellationToken cancellationToken);
    Task<(string Commit, string Version)> CheckAsync(ConfigSource source, string directory, CancellationToken cancellationToken);
}

internal sealed class ConfigSourceGitClient(ICliHostEnvironment hostEnvironment) : IConfigSourceGitClient
{
    public async Task<(string Commit, string Version, JsonObject Configuration)> AcquireAsync(ConfigSource source, string directory, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            var (repositoryDirectory, commit, version) = await FetchVersionAsync(source, directory, timeout.Token);
            var length = await RunAsync(repositoryDirectory, ["cat-file", "-s", version], timeout.Token);
            if (!long.TryParse(length.Trim(), CultureInfo.InvariantCulture, out var size) || size > 4 * 1024 * 1024)
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidConfiguration);
            }

            var json = await RunAsync(repositoryDirectory, ["cat-file", "blob", version], timeout.Token);
            try
            {
                var configuration = JsonNode.Parse(json, documentOptions: ConfigurationHelper.ParseOptions) as JsonObject
                    ?? throw new ConfigSourceException(ConfigSourceStrings.InvalidConfiguration);
                // Validate the known settings as well as JSON syntax. Unknown content settings
                // are retained for future schemas, but a source cannot enable another source.
                if (configuration.Any(p => p.Key.Equals("configSource", StringComparison.OrdinalIgnoreCase)
                    || p.Key.StartsWith("configSource:", StringComparison.OrdinalIgnoreCase)
                    || p.Key.StartsWith("configSource.", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ConfigSourceException(ConfigSourceStrings.NestedSource);
                }
                using var parsed = new ConfigurationManager();
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                parsed.AddJsonStream(stream);
                // Validate flat "features:enabled" keys using the same nested shape
                // as local CLI settings, rather than treating them as unknown settings.
                ConfigurationHelper.NormalizeSettings(configuration);
                _ = configuration.Deserialize(JsonSourceGenerationContext.Default.AspireConfigFile);
                return (commit, version, configuration);
            }
            catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException)
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidConfiguration);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConfigSourceException(ConfigSourceStrings.GitTimeout);
        }
    }

    public async Task<(string Commit, string Version)> CheckAsync(ConfigSource source, string directory, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            var (_, commit, version) = await FetchVersionAsync(source, directory, timeout.Token);
            return (commit, version);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConfigSourceException(ConfigSourceStrings.GitTimeout);
        }
    }

    private async Task<(string Directory, string Commit, string Version)> FetchVersionAsync(
        ConfigSource source, string directory, CancellationToken cancellationToken)
    {
        var repositoryDirectory = Path.Combine(directory, "repository.git");
        await RunAsync(directory, ["init", "--bare", "--", repositoryDirectory], cancellationToken);
        // Fetch accepts both a fully qualified branch ref and a full commit ID. Unlike
        // clone --branch, this also acquires commits outside the default branch's shallow history.
        // https://git-scm.com/docs/git-fetch
        await RunAsync(repositoryDirectory, ["fetch", "--depth", "1", "--no-tags", "--", source.Repository, source.GetGitRef()], cancellationToken);
        var commit = (await RunAsync(repositoryDirectory, ["rev-parse", "FETCH_HEAD^{commit}"], cancellationToken)).Trim();
        var treeEntry = await RunAsync(repositoryDirectory, ["ls-tree", "-z", commit, "--", source.Path], cancellationToken);
        // git ls-tree -z emits "100644 blob <object-id>\tpath\0". Inspecting the tree
        // identifies this file's version without reading or materializing its JSON.
        var tab = treeEntry.IndexOf('\t');
        var header = tab < 0 ? [] : treeEntry[..tab].Split(' ');
        if (header.Length != 3 || header[0] is not ("100644" or "100755") || header[1] != "blob"
            || treeEntry.IndexOf('\0') != treeEntry.Length - 1)
        {
            throw new ConfigSourceException(ConfigSourceStrings.MissingFile);
        }

        return (repositoryDirectory, commit, header[2]);
    }

    internal ProcessStartInfo CreateStartInfo(string directory, string[] arguments)
    {
        // Explicitly identify our bare repository. Discovery inside it is intentionally
        // disabled when a user sets safe.bareRepository=explicit.
        // https://git-scm.com/docs/git-config#Documentation/git-config.txt-safebareRepository
        var gitArguments = arguments[0] is "clone" or "init" ? arguments : [$"--git-dir={directory}", .. arguments];
        var startInfo = new ProcessStartInfo("git", ["--literal-pathspecs", .. gitArguments])
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            // Git prompts use the controlling terminal even though diagnostics are captured.
            // On Windows, keep that console attached when interactive input is allowed.
            CreateNoWindow = !hostEnvironment.SupportsInteractiveInput
        };
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = hostEnvironment.SupportsInteractiveInput ? "1" : "0";
        // Keep Git diagnostics stable for the local failure classification below.
        startInfo.Environment["LC_ALL"] = "C";
        foreach (var key in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES" })
        {
            startInfo.Environment.Remove(key);
        }
        return startInfo;
    }

    private async Task<string> RunAsync(string directory, string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(directory, arguments);
        try
        {
            var result = await Process.RunAndCaptureTextAsync(startInfo, cancellationToken);
            if (result.ExitStatus.ExitCode != 0)
            {
                // Git stderr can contain private repository URLs and credentials. Classify it
                // locally instead of copying it into exceptions, logs, or telemetry.
                var authenticationFailure = result.StandardError.Contains("authentication", StringComparison.OrdinalIgnoreCase)
                    || result.StandardError.Contains("permission denied", StringComparison.OrdinalIgnoreCase)
                    || result.StandardError.Contains("could not read Username", StringComparison.OrdinalIgnoreCase);
                throw new ConfigSourceException(authenticationFailure ? ConfigSourceStrings.AuthenticationFailed : ConfigSourceStrings.RepositoryUnavailable);
            }
            return result.StandardOutput;
        }
        catch (Win32Exception)
        {
            throw new ConfigSourceException(ConfigSourceStrings.GitNotFound);
        }
    }
}
