// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;

namespace Aspire.Hosting.CodeGeneration.Python;

/// <summary>
/// Provides language support for Python AppHosts.
/// Implements scaffolding, detection, and runtime configuration.
/// </summary>
public sealed class PythonLanguageSupport : ILanguageSupport
{
    /// <summary>
    /// The language/runtime identifier for Python.
    /// Format: {language}/{runtime} to support multiple runtimes (e.g., typescript/bun, typescript/deno).
    /// </summary>
    private const string LanguageId = "python";

    /// <summary>
    /// The code generation target language. This maps to the ICodeGenerator.Language property.
    /// </summary>
    private const string CodeGenTarget = "Python";

    private const string LanguageDisplayName = "Python";
    private static readonly string[] s_detectionPatterns = ["apphost.py"];

    /// <inheritdoc />
    public string Language => LanguageId;

    /// <inheritdoc />
    public Dictionary<string, string> Scaffold(ScaffoldRequest request)
    {
        var files = new Dictionary<string, string>();

        // Create apphost.py
        files["apphost.py"] = """
            # Aspire Python AppHost
            # For more information, see: https://aspire.dev

            from aspyre import create_builder

            with create_builder() as builder:
                # Add your resources here, for example:
                # redis = builder.add_container("cache", "redis:latest")
                # postgres = builder.add_postgres("db")
                builder.run()

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
                    "ASPNETCORE_ENVIRONMENT": "Development",
                    "DOTNET_ENVIRONMENT": "Development",
                    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:{{otlpPort}}",
                    "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:{{resourceServicePort}}"
                  }
                }
              }
            }
            """;

        return files;
    }

    /// <inheritdoc />
    public DetectionResult Detect(string directoryPath)
    {
        // Check for apphost.py
        var appHostPath = Path.Combine(directoryPath, "apphost.py");
        if (!File.Exists(appHostPath))
        {
            return DetectionResult.NotFound;
        }
        return DetectionResult.Found(LanguageId, "apphost.py");
    }

    /// <inheritdoc />
    public RuntimeSpec GetRuntimeSpec()
    {
        var pythonPath = FindPythonPath();
        return new RuntimeSpec
        {
            Language = LanguageId,
            DisplayName = LanguageDisplayName,
            CodeGenLanguage = CodeGenTarget,
            DetectionPatterns = s_detectionPatterns,
            Execute = new CommandSpec
            {
                Command = pythonPath,
                Args = ["{appHostFile}"]
            },
        };
    }

    private static string FindPythonPath()
    {
        // Try python3 first (preferred on Unix), then python
        if (PathLookupHelper.FindFullPathFromPath("python3") is not null)
        {
            return "python3";
        }
        return "python";
    }
}
