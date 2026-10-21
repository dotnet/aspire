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
    Task<(string Commit, JsonObject Configuration)> AcquireAsync(ConfigSource source, string directory, CancellationToken cancellationToken);
}

internal sealed class ConfigSourceGitClient(ICliHostEnvironment hostEnvironment) : IConfigSourceGitClient
{
    public async Task<(string Commit, JsonObject Configuration)> AcquireAsync(ConfigSource source, string directory, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            var repositoryDirectory = System.IO.Path.Combine(directory, "repository.git");
            await RunAsync(directory, ["clone", "--bare", "--depth", "1", "--no-tags", "--", source.Repository, repositoryDirectory], timeout.Token);
            var commit = (await RunAsync(repositoryDirectory, ["rev-parse", "HEAD"], timeout.Token)).Trim();
            var treeEntry = await RunAsync(repositoryDirectory, ["ls-tree", "-z", "HEAD", "--", source.Path], timeout.Token);
            // git ls-tree -z emits "100644 blob <object-id>\tpath\0". Reading the blob
            // directly avoids checkout filters, symlinks, and platform path interpretation.
            if (!treeEntry.StartsWith("100644 blob ", StringComparison.Ordinal)
                && !treeEntry.StartsWith("100755 blob ", StringComparison.Ordinal))
            {
                throw new ConfigSourceException(ConfigSourceStrings.MissingFile);
            }

            var objectName = $"HEAD:{source.Path}";
            var length = await RunAsync(repositoryDirectory, ["cat-file", "-s", objectName], timeout.Token);
            if (!long.TryParse(length.Trim(), CultureInfo.InvariantCulture, out var size) || size > 4 * 1024 * 1024)
            {
                throw new ConfigSourceException(ConfigSourceStrings.InvalidConfiguration);
            }

            var json = await RunAsync(repositoryDirectory, ["show", objectName], timeout.Token);
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
                return (commit, configuration);
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

    internal ProcessStartInfo CreateStartInfo(string directory, string[] arguments)
    {
        // Explicitly identify our bare repository. Discovery inside it is intentionally
        // disabled when a user sets safe.bareRepository=explicit.
        // https://git-scm.com/docs/git-config#Documentation/git-config.txt-safebareRepository
        var gitArguments = arguments[0] == "clone" ? arguments : [$"--git-dir={directory}", .. arguments];
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
