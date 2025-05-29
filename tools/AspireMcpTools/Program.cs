// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync().ConfigureAwait(false);

[McpServerToolType]
internal static class AspireProcessTools
{
    [McpServerTool, Description("Kills all instances of the 'aspire' CLI process.")]
    public static string KillAllAspireCliProcesses()
    {
        var processes = System.Diagnostics.Process.GetProcessesByName("aspire");
        var killedProcessIds = new List<int>();
        var failedProcessKills = new List<(int ProcessId, string ErrorMessage)>();

        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                killedProcessIds.Add(process.Id);
            }
            catch (Exception ex)
            {
                // This will probably never happen, but we catch it
                // and report just in case.
                failedProcessKills.Add((process.Id, ex.Message));
            }
        }

        var resultMessage = killedProcessIds.Count switch
        {
            0 => "No 'aspire' processes found.",
            1 => $"Killed 1 'aspire' process (PID: {killedProcessIds[0]}).",
            _ => $"Killed {killedProcessIds.Count} 'aspire' processes (PIDs: {string.Join(", ", killedProcessIds)})."
        };

        if (failedProcessKills.Count > 0)
        {
            var failedMessage = $"Failed to kill {failedProcessKills.Count} process(es): " +
                                $"{string.Join("; ", failedProcessKills.Select(fp => $"PID {fp.ProcessId}: {fp.ErrorMessage}"))}.";
            resultMessage += $" {failedMessage}";
        }

        return resultMessage;
    }

    [McpServerTool, Description("Creates a sandbox environment for testing the Aspire CLI with locally built packages. Builds Aspire, creates a new directory, and sets up NuGet configuration.")]
    public static async Task<string> CreateAspireSandboxAsync()
    {
        var workspaceRoot = "/workspaces/aspire";
        var workspacesDir = "/workspaces";
        var artifactsDir = Path.Combine(workspaceRoot, "artifacts");
        var packagesDir = Path.Combine(artifactsDir, "packages", "Debug", "Shipping");
        
        try
        {
            // Step 1: Run ./build.sh -restore -build -pack and wait for completion
            var buildResult = await RunProcessAsync("bash", "./build.sh -restore -build -pack /p:InstallBrowsersForPlaywright=false", workspaceRoot).ConfigureAwait(false);
            if (buildResult.ExitCode != 0)
            {
                if (!Directory.Exists(packagesDir))
                {
                    return $"Build failed with exit code {buildResult.ExitCode} and no packages were created. Output: {buildResult.Output}";
                }
                
                // Packages exist, so continue despite the non-zero exit code (likely Playwright issues)
            }

            // Step 2: Run dotnet build on src/Aspire.Cli
            var cliDir = Path.Combine(workspaceRoot, "src", "Aspire.Cli");
            var cliBuildResult = await RunProcessAsync("dotnet", "build", cliDir).ConfigureAwait(false);
            if (cliBuildResult.ExitCode != 0)
            {
                return $"CLI build failed with exit code {cliBuildResult.ExitCode}. Output: {cliBuildResult.Output}";
            }

            // Step 3: Create a new folder with unique name in /workspaces directory
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var sandboxName = $"aspire-sandbox-{timestamp}";
            var sandboxDir = Path.Combine(workspacesDir, sandboxName);
            Directory.CreateDirectory(sandboxDir);

            // Step 4: Copy /workspaces/aspire/NuGet.config to that directory
            var sourceNuGetConfig = Path.Combine(workspaceRoot, "NuGet.config");
            var targetNuGetConfig = Path.Combine(sandboxDir, "NuGet.config");
            File.Copy(sourceNuGetConfig, targetNuGetConfig);

            await UpdateNuGetConfigAsync(targetNuGetConfig, packagesDir).ConfigureAwait(false);

            return $"Sandbox created successfully at: {sandboxDir}\nPackages source: {packagesDir}\nTo use this sandbox, navigate to the directory and run Aspire CLI commands.";
        }
        catch (Exception ex)
        {
            return $"Failed to create sandbox: {ex.Message}";
        }
    }

    private static async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string arguments, string workingDirectory)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) => 
        { 
            if (e.Data is not null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) => 
        { 
            if (e.Data is not null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync().ConfigureAwait(false);

        var combinedOutput = outputBuilder.ToString() + errorBuilder.ToString();
        return (process.ExitCode, combinedOutput);
    }

    private static async Task UpdateNuGetConfigAsync(string nugetConfigPath, string packagesPath)
    {
        var content = await File.ReadAllTextAsync(nugetConfigPath).ConfigureAwait(false);
        
        // Add local-packages source after the existing sources
        var packageSourcesEndTag = "</packageSources>";
        var localPackageSource = $"""    <add key="local-packages" value="{packagesPath}" />""";
        
        content = content.Replace(packageSourcesEndTag, $"{localPackageSource}\n  {packageSourcesEndTag}");

        // Add package source mapping for Aspire* and Microsoft.Extensions.ServiceDiscovery* to local-packages
        var packageSourceMappingEndTag = "</packageSourceMapping>";
        var localPackageMapping = 
"""
    <packageSource key="local-packages">
      <package pattern="Aspire*" />
      <package pattern="Microsoft.Extensions.ServiceDiscovery*" />
    </packageSource>
""";
        
        content = content.Replace(packageSourceMappingEndTag, $"{localPackageMapping}  {packageSourceMappingEndTag}");

        await File.WriteAllTextAsync(nugetConfigPath, content).ConfigureAwait(false);
    }
}