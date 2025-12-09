// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Utils;

/// <summary>
/// Provides information about the CLI host environment capabilities.
/// </summary>
internal interface ICliHostEnvironment
{
    /// <summary>
    /// Gets whether the host supports interactive input (e.g., prompts, user input).
    /// </summary>
    bool SupportsInteractiveInput { get; }

    /// <summary>
    /// Gets whether the host supports interactive output (e.g., spinners, progress bars).
    /// </summary>
    bool SupportsInteractiveOutput { get; }

    /// <summary>
    /// Gets whether the host supports colors and ANSI codes.
    /// </summary>
    bool SupportsAnsi { get; }
}

/// <summary>
/// Default implementation that detects CLI host environment capabilities from configuration.
/// </summary>
internal sealed class CliHostEnvironment : ICliHostEnvironment
{
    // Common CI environment variables
    // https://github.com/watson/ci-info/blob/master/vendors.json
    private static readonly string[] s_ciEnvironmentVariables =
    [
        "CI", // Generic CI indicator
        "GITHUB_ACTIONS",
        "AZURE_PIPELINES",
        "TF_BUILD", // Azure Pipelines alternative
        "JENKINS_URL",
        "GITLAB_CI",
        "CIRCLECI",
        "TRAVIS",
        "BUILDKITE",
        "APPVEYOR",
        "TEAMCITY_VERSION",
        "BITBUCKET_BUILD_NUMBER",
        "CODEBUILD_BUILD_ID", // AWS CodeBuild
    ];

    /// <summary>
    /// Gets whether the host supports interactive input (e.g., prompts, user input).
    /// </summary>
    public bool SupportsInteractiveInput { get; }

    /// <summary>
    /// Gets whether the host supports interactive output (e.g., spinners, progress bars).
    /// </summary>
    public bool SupportsInteractiveOutput { get; }

    /// <summary>
    /// Gets whether the host supports colors and ANSI codes.
    /// </summary>
    public bool SupportsAnsi { get; }

    public CliHostEnvironment(IConfiguration configuration, bool nonInteractive)
    {
        // Check if ASPIRE_PLAYGROUND is set to force interactive mode
        var playgroundMode = IsPlaygroundMode(configuration);

        // If ASPIRE_PLAYGROUND is set, force interactive mode and ANSI support
        if (playgroundMode)
        {
            SupportsInteractiveInput = true;
            SupportsInteractiveOutput = true;
            SupportsAnsi = true;
        }
        // If --non-interactive is explicitly set, disable interactive input and output
        else if (nonInteractive)
        {
            SupportsInteractiveInput = false;
            SupportsInteractiveOutput = false;
            SupportsAnsi = DetectAnsiSupport(configuration);
        }
        else
        {
            SupportsInteractiveInput = DetectInteractiveInput(configuration);
            SupportsInteractiveOutput = DetectInteractiveOutput(configuration);
            SupportsAnsi = DetectAnsiSupport(configuration);
        }
    }

    private static bool DetectInteractiveInput(IConfiguration configuration)
    {
        // Check if explicitly disabled via configuration
        var nonInteractive = configuration["ASPIRE_NON_INTERACTIVE"];
        if (!string.IsNullOrEmpty(nonInteractive) &&
            (nonInteractive.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             nonInteractive.Equals("1", StringComparison.Ordinal)))
        {
            return false;
        }

        // Check if running in CI environment (no interactive input possible)
        if (IsCI(configuration))
        {
            return false;
        }

        return true;
    }

    private static bool DetectInteractiveOutput(IConfiguration configuration)
    {
        // Check if explicitly disabled via configuration
        var nonInteractive = configuration["ASPIRE_NON_INTERACTIVE"];
        if (!string.IsNullOrEmpty(nonInteractive) &&
            (nonInteractive.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             nonInteractive.Equals("1", StringComparison.Ordinal)))
        {
            return false;
        }

        // Check if running in CI environment (spinners pollute logs)
        if (IsCI(configuration))
        {
            return false;
        }

        return true;
    }

    private static bool DetectAnsiSupport(IConfiguration configuration)
    {
        // Check for ASPIRE_ANSI_PASS_THRU to force ANSI even when redirected
        if (IsAnsiPassThruEnabled(configuration))
        {
            return true;
        }

        // ANSI codes are supported even in CI environments for colored output
        // Only disable if explicitly configured
        var noColor = configuration["NO_COLOR"];
        if (!string.IsNullOrEmpty(noColor))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if ASPIRE_ANSI_PASS_THRU is enabled in configuration.
    /// </summary>
    internal static bool IsAnsiPassThruEnabled(IConfiguration configuration)
    {
        var ansiPassThru = configuration["ASPIRE_ANSI_PASS_THRU"];
        return !string.IsNullOrEmpty(ansiPassThru) &&
               (ansiPassThru.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                ansiPassThru.Equals("1", StringComparison.Ordinal));
    }

    private static bool IsCI(IConfiguration configuration)
    {
        foreach (var envVar in s_ciEnvironmentVariables)
        {
            var value = configuration[envVar];
            if (!string.IsNullOrEmpty(value))
            {
                // For CI variable, only return true if it's "true" or "1"
                if (envVar == "CI")
                {
                    return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           value.Equals("1", StringComparison.Ordinal);
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets whether the ASPIRE_PLAYGROUND environment variable is set to force interactive mode.
    /// </summary>
    internal static bool IsPlaygroundMode(IConfiguration configuration)
    {
        var playgroundMode = configuration["ASPIRE_PLAYGROUND"];
        return !string.IsNullOrEmpty(playgroundMode) &&
               playgroundMode.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
