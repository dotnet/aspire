// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Applies a git-based template by copying files with substitutions.
/// </summary>
internal sealed class GitTemplateEngine : IGitTemplateEngine
{
    private static readonly HashSet<string> s_binaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".webp", ".svg",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
        ".zip", ".gz", ".tar", ".7z", ".rar",
        ".dll", ".exe", ".so", ".dylib",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx",
        ".db", ".sqlite", ".mdb",
        ".pfx", ".p12", ".cer", ".pem"
    };

    private static readonly HashSet<string> s_excludedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "aspire-template.json"
    };

    private static readonly HashSet<string> s_excludedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".github"
    };

    private readonly ILogger<GitTemplateEngine> _logger;

    public GitTemplateEngine(ILogger<GitTemplateEngine> logger)
    {
        _logger = logger;
    }

    public async Task ApplyAsync(
        string templateDir,
        string outputDir,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(templateDir, "aspire-template.json");
        GitTemplateManifest? manifest = null;

        if (File.Exists(manifestPath))
        {
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            manifest = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateManifest);
        }

        // Build substitution maps from manifest + variables
        var filenameSubstitutions = manifest?.Substitutions?.Filenames ?? new Dictionary<string, string>();
        var contentSubstitutions = manifest?.Substitutions?.Content ?? new Dictionary<string, string>();
        var conditionalFiles = manifest?.ConditionalFiles ?? new Dictionary<string, string>();

        // Resolve substitution patterns → actual replacement values
        var resolvedFilenameMap = ResolveSubstitutions(filenameSubstitutions, variables);
        var resolvedContentMap = ResolveSubstitutions(contentSubstitutions, variables);

        Directory.CreateDirectory(outputDir);

        // Copy and transform files
        await CopyDirectoryAsync(
            templateDir, outputDir,
            resolvedFilenameMap, resolvedContentMap,
            conditionalFiles, variables,
            cancellationToken).ConfigureAwait(false);

        // Display post-creation messages
        if (manifest?.PostMessages is { Count: > 0 })
        {
            foreach (var message in manifest.PostMessages)
            {
                var evaluated = TemplateExpressionEvaluator.Evaluate(message, variables);
                _logger.LogInformation("{Message}", evaluated);
            }
        }
    }

    private static async Task CopyDirectoryAsync(
        string sourceDir,
        string destDir,
        Dictionary<string, string> filenameMap,
        Dictionary<string, string> contentMap,
        Dictionary<string, string> conditionalFiles,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken)
    {
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);

            if (s_excludedDirs.Contains(dirName))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDir, dir);
            if (IsExcludedByCondition(relativePath + "/", conditionalFiles, variables))
            {
                continue;
            }

            var destDirName = ApplyFilenameSubstitutions(dirName, filenameMap);
            var destSubDir = Path.Combine(destDir, destDirName);
            Directory.CreateDirectory(destSubDir);

            await CopyDirectoryAsync(dir, destSubDir, filenameMap, contentMap, conditionalFiles, variables, cancellationToken).ConfigureAwait(false);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);

            if (s_excludedFiles.Contains(fileName))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDir, file);
            if (IsExcludedByCondition(relativePath, conditionalFiles, variables))
            {
                continue;
            }

            var destFileName = ApplyFilenameSubstitutions(fileName, filenameMap);
            var destPath = Path.Combine(destDir, destFileName);

            if (IsBinaryFile(file))
            {
                File.Copy(file, destPath, overwrite: true);
            }
            else
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);

                foreach (var (pattern, replacement) in contentMap)
                {
                    content = content.Replace(pattern, replacement, StringComparison.Ordinal);
                }

                await File.WriteAllTextAsync(destPath, content, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static Dictionary<string, string> ResolveSubstitutions(
        Dictionary<string, string> substitutionMap,
        IReadOnlyDictionary<string, string> variables)
    {
        var resolved = new Dictionary<string, string>();

        foreach (var (pattern, expression) in substitutionMap)
        {
            resolved[pattern] = TemplateExpressionEvaluator.Evaluate(expression, variables);
        }

        return resolved;
    }

    private static string ApplyFilenameSubstitutions(string name, Dictionary<string, string> filenameMap)
    {
        foreach (var (pattern, replacement) in filenameMap)
        {
            name = name.Replace(pattern, replacement, StringComparison.Ordinal);
        }

        return name;
    }

    private static bool IsExcludedByCondition(
        string relativePath,
        Dictionary<string, string> conditionalFiles,
        IReadOnlyDictionary<string, string> variables)
    {
        foreach (var (pathPattern, expression) in conditionalFiles)
        {
            if (!relativePath.StartsWith(pathPattern, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(relativePath, pathPattern, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = TemplateExpressionEvaluator.Evaluate(expression, variables);

            // Exclude if the condition evaluates to false/empty
            if (string.IsNullOrEmpty(value) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBinaryFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (s_binaryExtensions.Contains(ext))
        {
            return true;
        }

        // Null-byte sniff for unknown extensions
        try
        {
            var buffer = new byte[8192];
            using var stream = File.OpenRead(filePath);
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            for (var i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0)
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't read it, treat as binary to be safe.
            return true;
        }

        return false;
    }
}
