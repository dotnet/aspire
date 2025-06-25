// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal sealed class VersionChecker : BackgroundService
{
    private readonly IInteractionService _interactionService;

    public VersionChecker(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "package search Aspire.Hosting.AppHost --format json",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = processStartInfo
        };

        process.Start();

        // Read the output
        string output = await process.StandardOutput.ReadToEndAsync(stoppingToken).ConfigureAwait(false);
        string error = await process.StandardError.ReadToEndAsync(stoppingToken).ConfigureAwait(false);

        await process.WaitForExitAsync(stoppingToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to execute command: {processStartInfo.FileName} {processStartInfo.Arguments}. Error: {error}");
        }

        var packages = ParseOutput(output);

        var assemblyVersion = typeof(VersionChecker).Assembly.GetName().Version;

        if (packages.Count > 0)
        {
            var result = await _interactionService.PromptMessageBarAsync(
                title: "Update now",
                message: $"Aspire {packages[0].LatestVersion} is available.",
                options: new MessageBarInteractionOptions
                {
                    LinkText = "Upgrade instructions",
                    PrimaryButtonText = "Ignore"
                },
                cancellationToken: stoppingToken).ConfigureAwait(false);

            if (result.Data)
            {
                //
            }
        }
    }

    private static List<Package> ParseOutput(string outputJson)
    {
        var packages = new List<Package>();

        using var document = JsonDocument.Parse(outputJson);
        var root = document.RootElement;

        if (root.TryGetProperty("searchResult", out var searchResults))
        {
            foreach (var result in searchResults.EnumerateArray())
            {
                if (result.TryGetProperty("packages", out var packagesArray))
                {
                    foreach (var pkg in packagesArray.EnumerateArray())
                    {
                        var id = pkg.GetProperty("id").GetString();
                        var latestVersion = pkg.GetProperty("latestVersion").GetString();
                        if (id != null && latestVersion != null)
                        {
                            packages.Add(new Package(id, latestVersion));
                        }
                    }
                }
            }
        }

        return packages;
    }

    private record Package(string Id, string LatestVersion);
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
