// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;

namespace Aspire.Hosting.CodeGeneration.Python;

/// <summary>
/// Provides language support for Python AppHosts, implementing scaffolding, detection, and runtime configuration.
/// </summary>
/// <remarks>
/// This implementation generates the files required for a Python-based Aspire AppHost and configures
/// the runtime to create a virtual environment and install dependencies via <c>uv</c>,
/// and execute the AppHost with <c>uv run python</c>.
/// </remarks>
public sealed class PythonLanguageSupport : ILanguageSupport
{
    /// <summary>
    /// The language/runtime identifier for Python.
    /// </summary>
    private const string LanguageId = "python";

    /// <summary>
    /// The code generation target language. This maps to the ICodeGenerator.Language property.
    /// </summary>
    private const string CodeGenTarget = "Python";

    private const string LanguageDisplayName = "Python";
    private static readonly string[] s_detectionPatterns = ["apphost.py"];

    /// <summary>
    /// Gets the language identifier for Python AppHosts.
    /// </summary>
    /// <value>The string <c>"python"</c>.</value>
    public string Language => LanguageId;

    /// <summary>
    /// Generates the initial scaffold files for a new Python AppHost project.
    /// </summary>
    /// <param name="request">The scaffold request containing project details such as the project name and an optional port seed.</param>
    /// <returns>
    /// A dictionary mapping relative file paths to their contents. The generated files include
    /// <c>apphost.py</c>, <c>pylock.toml</c>, and <c>apphost.run.json</c>.
    /// </returns>
    /// <remarks>
    /// The <c>apphost.run.json</c> file is generated with randomly assigned port numbers unless
    /// <see cref="ScaffoldRequest.PortSeed"/> is provided, in which case ports are deterministically assigned.
    /// </remarks>
    public Dictionary<string, string> Scaffold(ScaffoldRequest request)
    {
        var files = new Dictionary<string, string>();

        // Create apphost.py
        files["apphost.py"] = """
            # Aspire Python AppHost
            # For more information, see: https://aspire.dev

            from aspire_app import create_builder

            with create_builder() as builder:
                # Add your resources here, for example:
                # redis = builder.add_container("cache", "redis:latest")
                # postgres = builder.add_postgres("db")
                builder.run()
            """;

        // Create pylock.toml
        var version = typeof(PythonLanguageSupport).Assembly.GetName().Version?.ToString() ?? "0.1.0";
        var generatedPath = request.GeneratedFolderPath ?? ".aspire/python";
        files["pylock.toml"] = $$"""
            # Aspire Python AppHost requirements
            requires-python = '>=3.11'

            [[packages]]
            name = "aspire_app"
            version = "{{version}}"
            editable = true

            [packages.directory]
            path = "{{generatedPath}}"
            """;

        // Create apphost.run.json with random ports
        // Use PortSeed if provided (for testing), otherwise use random
        var random = request.PortSeed.HasValue
            ? new Random(request.PortSeed.Value)
            : Random.Shared;

        var httpsPort = random.Next(10000, 65000);
        var httpPort = random.Next(10000, 65000);
        var otlpPort = random.Next(10000, 65000);
        var resourceServicePort = random.Next(10000, 65000);

        files["apphost.run.json"] = $$"""
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:{{httpsPort}};http://localhost:{{httpPort}}",
                  "environmentVariables": {
                    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:{{otlpPort}}",
                    "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:{{resourceServicePort}}"
                  }
                }
              }
            }
            """;

        return files;
    }

    /// <summary>
    /// Detects whether the specified directory contains a Python AppHost project.
    /// </summary>
    /// <param name="directoryPath">The full path to the directory to inspect.</param>
    /// <returns>
    /// A <see cref="DetectionResult"/> with <see cref="DetectionResult.Found"/> if both <c>apphost.py</c>
    /// and <c>pylock.toml</c> exist in <paramref name="directoryPath"/>; otherwise <see cref="DetectionResult.NotFound"/>.
    /// </returns>
    public DetectionResult Detect(string directoryPath)
    {
        var appHostPath = Path.Combine(directoryPath, "apphost.py");
        if (!File.Exists(appHostPath))
        {
            return DetectionResult.NotFound;
        }

        var pylockPath = Path.Combine(directoryPath, "pylock.toml");
        if (!File.Exists(pylockPath))
        {
            return DetectionResult.NotFound;
        }

        return DetectionResult.Found(LanguageId, "apphost.py");
    }

    /// <summary>
    /// Gets the runtime execution specification for Python AppHosts.
    /// </summary>
    /// <returns>
    /// A <see cref="RuntimeSpec"/> that configures initialization via <c>uv venv</c> and <c>uv pip sync</c>,
    /// runtime dependency installation via <c>uv pip sync</c>, and AppHost execution via
    /// <c>uv run python {appHostFile}</c>.
    /// </returns>
    public RuntimeSpec GetRuntimeSpec()
    {
        return new RuntimeSpec
        {
            Language = LanguageId,
            DisplayName = LanguageDisplayName,
            CodeGenLanguage = CodeGenTarget,
            DetectionPatterns = s_detectionPatterns,
            Initialize =
            [
                new CommandSpec
                {
                    Command = "uv",
                    Args = ["venv", ".venv"]
                }
            ],
            InstallDependencies = new CommandSpec
            {
                Command = "uv",
                Args = ["pip", "sync", "pylock.toml"]
            },
            Execute = new CommandSpec
            {
                Command = "uv",
                Args = ["run", "python", "{appHostFile}"]
            }
        };
    }
}
