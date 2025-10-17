// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Utils;

/// <summary>
/// Detects if the current process is running in a CI environment.
/// </summary>
internal interface ICIEnvironmentDetector
{
    /// <summary>
    /// Gets whether the current process is running in a CI environment.
    /// </summary>
    bool IsCI { get; }
}

/// <summary>
/// Default implementation that detects CI environments from configuration.
/// </summary>
internal sealed class CIEnvironmentDetector(IConfiguration configuration) : ICIEnvironmentDetector
{
    /// <summary>
    /// Gets whether the current process is running in a CI environment.
    /// </summary>
    public bool IsCI { get; } = DetectCI(configuration);

    private static bool DetectCI(IConfiguration configuration)
    {
        // Check for common CI environment variables
        // https://github.com/watson/ci-info/blob/master/vendors.json
        var ciEnvVars = new[]
        {
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
        };

        foreach (var envVar in ciEnvVars)
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
}
