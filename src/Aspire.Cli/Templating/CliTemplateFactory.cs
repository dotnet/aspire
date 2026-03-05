// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Scaffolding;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating;

internal sealed partial class CliTemplateFactory : ITemplateFactory
{
    private static readonly HashSet<string> s_binaryTemplateExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".ico",
        ".bmp",
        ".webp",
        ".svg",
        ".woff",
        ".woff2",
        ".ttf",
        ".otf"
    ];

    private static readonly Option<bool?> s_localhostTldOption = new("--localhost-tld")
    {
        Description = TemplatingStrings.UseLocalhostTld_Description
    };

    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IScaffoldingService _scaffoldingService;
    private readonly INewCommandPrompter _prompter;
    private readonly CliExecutionContext _executionContext;
    private readonly IInteractionService _interactionService;
    private readonly ICliHostEnvironment _hostEnvironment;
    private readonly TemplateNuGetConfigService _templateNuGetConfigService;
    private readonly ILogger<CliTemplateFactory> _logger;

    public CliTemplateFactory(
        ILanguageDiscovery languageDiscovery,
        IScaffoldingService scaffoldingService,
        INewCommandPrompter prompter,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        ICliHostEnvironment hostEnvironment,
        TemplateNuGetConfigService templateNuGetConfigService,
        ILogger<CliTemplateFactory> logger)
    {
        _languageDiscovery = languageDiscovery;
        _scaffoldingService = scaffoldingService;
        _prompter = prompter;
        _executionContext = executionContext;
        _interactionService = interactionService;
        _hostEnvironment = hostEnvironment;
        _templateNuGetConfigService = templateNuGetConfigService;
        _logger = logger;
    }

    public Task<IEnumerable<ITemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<ITemplate> templates =
        [
            new CallbackTemplate(
                KnownTemplateId.TypeScriptStarter,
                "Starter App (Express/React)",
                projectName => $"./{projectName}",
                static cmd => AddOptionIfMissing(cmd, s_localhostTldOption),
                ApplyTypeScriptStarterTemplateAsync,
                runtime: TemplateRuntime.Cli,
                supportsLanguageCallback: static languageId =>
                    languageId.Equals(KnownLanguageId.TypeScript, StringComparison.OrdinalIgnoreCase) ||
                    languageId.Equals(KnownLanguageId.TypeScriptAlias, StringComparison.OrdinalIgnoreCase)),

            new CallbackTemplate(
                KnownTemplateId.EmptyAppHost,
                "Empty AppHost",
                projectName => $"./{projectName}",
                static cmd => AddOptionIfMissing(cmd, s_localhostTldOption),
                ApplyEmptyAppHostTemplateAsync,
                runtime: TemplateRuntime.Cli,
                supportsLanguageCallback: static languageId =>
                    languageId.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase) ||
                    languageId.Equals(KnownLanguageId.TypeScript, StringComparison.OrdinalIgnoreCase) ||
                    languageId.Equals(KnownLanguageId.TypeScriptAlias, StringComparison.OrdinalIgnoreCase),
                selectableAppHostLanguages: [KnownLanguageId.CSharp, KnownLanguageId.TypeScript])
        ];

        return Task.FromResult(templates);
    }

    public Task<IEnumerable<ITemplate>> GetInitTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ITemplate>>([]);
    }

    private static string ApplyTokens(string content, string projectName, string projectNameLower, string aspireVersion, TemplatePorts ports, string hostName = "localhost")
    {
        return content
            .Replace("{{projectName}}", projectName)
            .Replace("{{projectNameLower}}", projectNameLower)
            .Replace("{{aspireVersion}}", aspireVersion)
            .Replace("{{hostName}}", hostName)
            .Replace("{{httpPort}}", ports.HttpPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{httpsPort}}", ports.HttpsPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{otlpHttpPort}}", ports.OtlpHttpPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{otlpHttpsPort}}", ports.OtlpHttpsPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{mcpHttpPort}}", ports.McpHttpPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{mcpHttpsPort}}", ports.McpHttpsPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{resourceHttpPort}}", ports.ResourceHttpPort.ToString(CultureInfo.InvariantCulture))
            .Replace("{{resourceHttpsPort}}", ports.ResourceHttpsPort.ToString(CultureInfo.InvariantCulture));
    }

    private static TemplatePorts GenerateRandomPorts()
    {
        return new TemplatePorts(
            HttpPort: Random.Shared.Next(15000, 15300),
            HttpsPort: Random.Shared.Next(17000, 17300),
            OtlpHttpPort: Random.Shared.Next(19000, 19300),
            OtlpHttpsPort: Random.Shared.Next(21000, 21300),
            McpHttpPort: Random.Shared.Next(18000, 18300),
            McpHttpsPort: Random.Shared.Next(23000, 23300),
            ResourceHttpPort: Random.Shared.Next(20000, 20300),
            ResourceHttpsPort: Random.Shared.Next(22000, 22300));
    }

    private sealed record TemplatePorts(
        int HttpPort, int HttpsPort,
        int OtlpHttpPort, int OtlpHttpsPort,
        int McpHttpPort, int McpHttpsPort,
        int ResourceHttpPort, int ResourceHttpsPort);

    private static void AddOptionIfMissing(System.CommandLine.Command command, System.CommandLine.Option option)
    {
        if (!command.Options.Contains(option))
        {
            command.Options.Add(option);
        }
    }

    private async Task CopyTemplateTreeToDiskAsync(string templateRoot, string outputPath, Func<string, string> tokenReplacer, CancellationToken cancellationToken)
    {
        var assembly = typeof(CliTemplateFactory).Assembly;
        _logger.LogDebug("Copying embedded template tree '{TemplateRoot}' to '{OutputPath}'.", templateRoot, outputPath);

        var allResourceNames = assembly.GetManifestResourceNames();
        var resourcePrefix = $"{templateRoot}.";
        var resourceNames = allResourceNames
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            _logger.LogDebug("No embedded resources found for template root '{TemplateRoot}'. Available manifest resources: {ManifestResources}", templateRoot, string.Join(", ", allResourceNames));
            throw new InvalidOperationException($"No embedded template resources found for '{templateRoot}'.");
        }

        _logger.LogDebug("Found {ResourceCount} embedded resources for template root '{TemplateRoot}': {TemplateResources}", resourceNames.Length, templateRoot, string.Join(", ", resourceNames));

        foreach (var resourceName in resourceNames)
        {
            var relativePath = resourceName[resourcePrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(outputPath, relativePath);
            var fileDirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded template resource not found: {resourceName}");

            _logger.LogDebug("Writing embedded template resource '{ResourceName}' to '{FilePath}'.", resourceName, filePath);
            if (s_binaryTemplateExtensions.Contains(Path.GetExtension(filePath)))
            {
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            else
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync(cancellationToken);
                var transformedContent = tokenReplacer(content);
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var writer = new StreamWriter(fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                await writer.WriteAsync(transformedContent.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
        }
    }

    private void DisplayProcessOutput(ProcessExecutionResult result, bool treatStandardErrorAsError)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _interactionService.DisplaySubtleMessage(result.StandardOutput.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            var message = result.StandardError.TrimEnd();
            if (treatStandardErrorAsError)
            {
                _interactionService.DisplayError(message);
            }
            else
            {
                _interactionService.DisplaySubtleMessage(message);
            }
        }
    }

    private static async Task<ProcessExecutionResult> RunProcessAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.StandardInput.Close(); // Prevent hanging on prompts

        // Drain output streams to prevent deadlocks
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }

            throw;
        }

        return new ProcessExecutionResult(
            process.ExitCode,
            await outputTask.ConfigureAwait(false),
            await errorTask.ConfigureAwait(false));
    }

    private sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
}
