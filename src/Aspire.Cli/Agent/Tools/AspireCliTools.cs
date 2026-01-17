// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Agent.Tools;

/// <summary>
/// Implementation of the aspire new tool.
/// </summary>
internal sealed class AspireNewTool : IAspireNewTool
{
    private readonly CliExecutionContext _executionContext;

    public AspireNewTool(CliExecutionContext executionContext)
    {
        _executionContext = executionContext;
    }

    public async Task<string> ExecuteAsync(string template, string name, string? outputDir)
    {
        var args = $"new {template} --name {name}";
        if (!string.IsNullOrEmpty(outputDir))
        {
            args += $" --output \"{outputDir}\"";
        }

        var result = await RunAspireCommandAsync(args);
        return result.Success
            ? $"Successfully created project '{name}' from template '{template}'.\n{result.Output}"
            : $"Failed to create project: {result.Error}";
    }

    private async Task<(bool Success, string Output, string Error)> RunAspireCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "aspire",
            Arguments = arguments,
            WorkingDirectory = _executionContext.WorkingDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode == 0, output, error);
    }
}

/// <summary>
/// Implementation of the aspire add tool.
/// </summary>
internal sealed class AspireAddTool : IAspireAddTool
{
    private readonly CliExecutionContext _executionContext;

    public AspireAddTool(CliExecutionContext executionContext)
    {
        _executionContext = executionContext;
    }

    public async Task<string> ExecuteAsync(string integration, string appHostPath)
    {
        var args = $"add {integration} --project \"{appHostPath}\" --non-interactive";

        var result = await RunAspireCommandAsync(args);
        return result.Success
            ? $"Successfully added '{integration}' to the AppHost.\n{result.Output}"
            : $"Failed to add integration: {result.Error}";
    }

    private async Task<(bool Success, string Output, string Error)> RunAspireCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "aspire",
            Arguments = arguments,
            WorkingDirectory = _executionContext.WorkingDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode == 0, output, error);
    }
}

/// <summary>
/// Implementation of the aspire run tool.
/// </summary>
internal sealed class AspireRunTool : IAspireRunTool
{
    private readonly CliExecutionContext _executionContext;

    public AspireRunTool(CliExecutionContext executionContext)
    {
        _executionContext = executionContext;
    }

    public async Task<string> ExecuteAsync(string appHostPath, bool watch)
    {
        var args = $"run --project \"{appHostPath}\"";
        if (watch)
        {
            args += " --watch";
        }

        // For run, we start the process but don't wait for it to complete
        // Return info about how to stop it
        var startInfo = new ProcessStartInfo
        {
            FileName = "aspire",
            Arguments = args,
            WorkingDirectory = _executionContext.WorkingDirectory.FullName,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        Process.Start(startInfo);

        await Task.Delay(2000); // Give it time to start

        return $"Started AppHost. The dashboard should be available shortly. Use Ctrl+C in the terminal to stop.";
    }
}

/// <summary>
/// Implementation of the aspire doctor tool.
/// </summary>
internal sealed class AspireDoctorTool : IAspireDoctorTool
{
    private readonly CliExecutionContext _executionContext;

    public AspireDoctorTool(CliExecutionContext executionContext)
    {
        _executionContext = executionContext;
    }

    public async Task<string> ExecuteAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "aspire",
            Arguments = "doctor --non-interactive",
            WorkingDirectory = _executionContext.WorkingDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return process.ExitCode == 0
            ? $"Environment check completed:\n{output}"
            : $"Environment check found issues:\n{output}\n{error}";
    }
}
