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

    public async Task<bool> FetchAsync(ResolvedTemplate resolved, string targetDir, CancellationToken cancellationToken = default)
    {
        var repo = resolved.EffectiveRepo;
        var templatePath = resolved.Entry.Path;

        // For local sources, copy files directly.
        if (Uri.TryCreate(repo, UriKind.Absolute, out var uri) && uri.IsFile || (!repo.Contains("://", StringComparison.Ordinal) && !repo.Contains('@') && Directory.Exists(repo)))
        {
            var sourcePath = Path.Combine(repo, templatePath);
            if (!Directory.Exists(sourcePath))
            {
                _logger.LogError("Local template path does not exist: {Path}", sourcePath);
                return false;
            }
            CopyDirectory(sourcePath, targetDir);
            return true;
        }

        var gitRef = resolved.Source.Ref ?? "HEAD";
        Directory.CreateDirectory(targetDir);

        try
        {
            var cloneResult = await RunGitAsync(
                targetDir,
                ["clone", "--depth", "1", "--branch", gitRef, "--sparse", "--filter=blob:none", repo, "."],
                cancellationToken).ConfigureAwait(false);

            if (cloneResult != 0)
            {
                cloneResult = await RunGitAsync(
                    targetDir,
                    ["clone", "--depth", "1", "--sparse", "--filter=blob:none", repo, "."],
                    cancellationToken).ConfigureAwait(false);

                if (cloneResult != 0)
                {
                    _logger.LogError("Failed to clone repository {Repo}.", repo);
                    return false;
                }

                await RunGitAsync(targetDir, ["fetch", "origin", gitRef], cancellationToken).ConfigureAwait(false);
                await RunGitAsync(targetDir, ["checkout", "FETCH_HEAD"], cancellationToken).ConfigureAwait(false);
            }

            if (templatePath is not "." and not "")
            {
                await RunGitAsync(
                    targetDir,
                    ["sparse-checkout", "set", templatePath],
                    cancellationToken).ConfigureAwait(false);
            }

            // Move template files from subdirectory to target root if needed
            var templateSubDir = Path.Combine(targetDir, templatePath);
            if (templatePath is not "." and not "" && Directory.Exists(templateSubDir))
            {
                var tempMove = targetDir + "_move";
                Directory.Move(templateSubDir, tempMove);

                foreach (var entry in Directory.GetFileSystemEntries(targetDir))
                {
                    var name = Path.GetFileName(entry);
                    if (name == ".git" || entry == tempMove)
                    {
                        continue;
                    }
                    if (Directory.Exists(entry))
                    {
                        Directory.Delete(entry, recursive: true);
                    }
                    else
                    {
                        File.Delete(entry);
                    }
                }

                foreach (var entry in Directory.GetFileSystemEntries(tempMove))
                {
                    var dest = Path.Combine(targetDir, Path.GetFileName(entry));
                    if (Directory.Exists(entry))
                    {
                        Directory.Move(entry, dest);
                    }
                    else
                    {
                        File.Move(entry, dest);
                    }
                }
                Directory.Delete(tempMove, recursive: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch template from {Repo}.", repo);
            return false;
        }
    }

    private static async Task<int> RunGitAsync(string workingDir, string[] args, CancellationToken cancellationToken)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = System.Diagnostics.Process.Start(psi);
        if (process is null)
        {
            return -1;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (s_excludedDirs.Contains(name))
            {
                continue;
            }
            Directory.CreateDirectory(Path.Combine(targetDir, Path.GetRelativePath(sourceDir, dir)));
        }
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            // Skip files in excluded dirs
            var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Any(s_excludedDirs.Contains))
            {
                continue;
            }
            File.Copy(file, Path.Combine(targetDir, relativePath), overwrite: true);
        }
    }
}
