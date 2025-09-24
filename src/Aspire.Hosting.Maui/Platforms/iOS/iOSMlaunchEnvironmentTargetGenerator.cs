// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml.Linq;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Maui.Platforms.iOS;

internal static class iOSMlaunchEnvironmentTargetGenerator
{
    private const string PlatformMoniker = "ios";

    public static async Task AppendEnvironmentTargetsAsync(CommandLineArgsCallbackContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Resource is not ProjectResource projectResource)
        {
            return;
        }

        var generator = new Generator(context, projectResource);
        await generator.ExecuteAsync(context.CancellationToken).ConfigureAwait(false);
    }

    private sealed class Generator
    {
        private readonly CommandLineArgsCallbackContext _context;
        private readonly ProjectResource _projectResource;
        private readonly Dictionary<string, string> _environment = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _encodedKeys = new(StringComparer.OrdinalIgnoreCase);

        public Generator(CommandLineArgsCallbackContext context, ProjectResource projectResource)
        {
            _context = context;
            _projectResource = projectResource;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await CollectEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            if (_environment.Count == 0)
            {
                return;
            }

            var logger = _context.Logger;

            var targetsPath = CreateTargetsFile(logger);
            _context.Args.Add("-p:CustomAfterMicrosoftCommonTargets=" + targetsPath);

            logger.LogInformation(
                "Forwarding {EnvironmentVariableCount} environment variable(s) to the {Platform} launcher using targets file '{TargetsFile}'.",
                _environment.Count,
                PlatformMoniker,
                targetsPath);

            if (_encodedKeys.Count > 0)
            {
                logger.LogInformation(
                    "Encoded semicolons for environment variables {EnvironmentVariables} when forwarding to '{Resource}'.",
                    string.Join(", ", _encodedKeys),
                    _projectResource.Name);
            }
        }

        private async Task CollectEnvironmentAsync(CancellationToken cancellationToken)
        {
            await _projectResource.ProcessEnvironmentVariableValuesAsync(
                _context.ExecutionContext,
                (key, _, processed, exception) =>
                {
                    if (exception is not null || string.IsNullOrEmpty(key) || processed is not string value)
                    {
                        return;
                    }

                    if (!ShouldForwardToMlaunch(key))
                    {
                        return;
                    }

                    var encodedValue = EncodeSemicolons(value, out var wasEncoded);
                    _environment[key] = encodedValue;
                    if (wasEncoded)
                    {
                        _encodedKeys.Add(key);
                    }
                },
                _context.Logger,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private string CreateTargetsFile(ILogger logger)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "aspire", "maui", "mlaunch-env");
            Directory.CreateDirectory(tempDirectory);

            PruneOldTargets(tempDirectory, logger);

            var sanitizedName = SanitizeFileName(_projectResource.Name + "-" + PlatformMoniker);
            var uniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            var targetsPath = Path.Combine(tempDirectory, $"{sanitizedName}-{uniqueId}.targets");

            var projectElement = new XElement("Project");
            projectElement.Add(new XElement(
                "Import",
                new XAttribute("Project", "$(MSBuildExtensionsPath)/v$(MSBuildToolsVersion)/Custom.After.Microsoft.Common.targets"),
                new XAttribute("Condition", "Exists('$(MSBuildExtensionsPath)/v$(MSBuildToolsVersion)/Custom.After.Microsoft.Common.targets')")));

            var itemGroup = new XElement("ItemGroup");
            foreach (var (key, value) in _environment.OrderBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                itemGroup.Add(new XElement("MlaunchEnvironmentVariables", new XAttribute("Include", $"{key}={value}")));
            }

            projectElement.Add(itemGroup);

            projectElement.Add(new XElement(
                "Target",
                new XAttribute("Name", "AspireLogMlaunchEnvironmentVariables"),
                new XAttribute("AfterTargets", "PrepareForBuild"),
                new XAttribute("Condition", "'@(MlaunchEnvironmentVariables)' != ''"),
                new XElement(
                    "Message",
                    new XAttribute("Importance", "High"),
                    new XAttribute("Text", "Aspire forwarding mlaunch environment variables: @(MlaunchEnvironmentVariables, ', ')")
                )));

            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), projectElement);
            document.Save(targetsPath);

            return targetsPath;
        }

        private static void PruneOldTargets(string directory, ILogger logger)
        {
            var expiration = DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
            var deletedFiles = new List<string>();

            foreach (var file in Directory.EnumerateFiles(directory, "*.targets", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.Exists && info.LastWriteTimeUtc < expiration)
                    {
                        info.Delete();
                        deletedFiles.Add(info.Name);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to prune stale mlaunch targets file '{TargetsFile}'.", file);
                }
            }

            if (deletedFiles.Count > 0)
            {
                logger.LogDebug("Pruned {Count} stale mlaunch targets file(s) from '{Directory}': {Files}.", deletedFiles.Count, directory, string.Join(", ", deletedFiles));
            }
        }

        private static bool ShouldForwardToMlaunch(string key)
        {
            return key.StartsWith("services__", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("connectionstrings__", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("ASPIRE_", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("AppHost__", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("OTEL_", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("LOGGING__CONSOLE", StringComparison.OrdinalIgnoreCase)
                || key.Equals("ASPNETCORE_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
                || key.Equals("ASPNETCORE_URLS", StringComparison.OrdinalIgnoreCase)
                || key.Equals("DOTNET_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
                || key.Equals("DOTNET_URLS", StringComparison.OrdinalIgnoreCase)
                || key.Equals("DOTNET_LAUNCH_PROFILE", StringComparison.OrdinalIgnoreCase)
                || key.Equals("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            if (name.IndexOfAny(invalidCharacters) < 0)
            {
                return name;
            }

            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalidCharacters, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static string EncodeSemicolons(string value, out bool wasEncoded)
        {
            wasEncoded = value.Contains(';', StringComparison.Ordinal);
            if (!wasEncoded)
            {
                return value;
            }

            return value.Replace(";", "%3B", StringComparison.Ordinal);
        }
    }
}
