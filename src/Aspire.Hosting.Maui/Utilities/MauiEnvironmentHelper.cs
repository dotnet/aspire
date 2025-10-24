// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Maui.Utilities;

/// <summary>
/// Provides utilities for handling environment variables in MAUI projects.
/// </summary>
/// <remarks>
/// Some MAUI platforms (like Android and iOS) require environment variables to be passed via
/// an intermediate MSBuild targets file rather than directly through the process environment.
/// This class provides reusable infrastructure for generating these targets files.
/// </remarks>
internal static class MauiEnvironmentHelper
{
    /// <summary>
    /// Creates an MSBuild targets file for Android that sets environment variables.
    /// </summary>
    /// <param name="resource">The resource to collect environment variables from.</param>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the generated targets file, or null if no environment variables are present.</returns>
    public static async Task<string?> CreateAndroidEnvironmentTargetsFileAsync(
        IResource resource,
        DistributedApplicationExecutionContext executionContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var encodedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Collect all environment variables from the resource
        await resource.ProcessEnvironmentVariableValuesAsync(
            executionContext,
            (key, unprocessed, processed, ex) =>
            {
                if (ex is not null || string.IsNullOrEmpty(key) || processed is not string value)
                {
                    return;
                }

                // Android environment variables must be uppercase to be properly read by the runtime
                var normalizedKey = key.ToUpperInvariant();
                var encodedValue = EncodeSemicolons(value, out var wasEncoded);

                environmentVariables[normalizedKey] = encodedValue;

                if (wasEncoded)
                {
                    encodedKeys.Add(normalizedKey);
                }
            },
            logger,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        // If no environment variables, return null
        if (environmentVariables.Count == 0)
        {
            return null;
        }

        // Create a temporary targets file
        var tempDirectory = Path.Combine(Path.GetTempPath(), "aspire", "maui", "android-env");
        Directory.CreateDirectory(tempDirectory);

        // Prune old targets files
        PruneOldTargets(tempDirectory, logger);

        var sanitizedName = SanitizeFileName(resource.Name + "-android");
        var uniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var targetsFilePath = Path.Combine(tempDirectory, $"{sanitizedName}-{uniqueId}.targets");

        // Generate the targets file content
        var targetsContent = GenerateAndroidTargetsFileContent(environmentVariables);

        // Write the file
        await File.WriteAllTextAsync(targetsFilePath, targetsContent, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Created Android environment targets file: {Path}", targetsFilePath);
        logger.LogDebug("Environment variables: {Count}", environmentVariables.Count);

        if (encodedKeys.Count > 0)
        {
            logger.LogDebug("Encoded semicolons for environment variables: {Keys}", string.Join(", ", encodedKeys));
        }

        return targetsFilePath;
    }

    /// <summary>
    /// Generates the content of an MSBuild targets file for Android environment variables.
    /// </summary>
    private static string GenerateAndroidTargetsFileContent(Dictionary<string, string> environmentVariables)
    {
        var projectElement = new XElement("Project");

        // Import the standard Custom.After.Microsoft.Common.targets if it exists
        projectElement.Add(new XElement(
            "Import",
            new XAttribute("Project", "$(MSBuildExtensionsPath)/v$(MSBuildToolsVersion)/Custom.After.Microsoft.Common.targets"),
            new XAttribute("Condition", "Exists('$(MSBuildExtensionsPath)/v$(MSBuildToolsVersion)/Custom.After.Microsoft.Common.targets')")
        ));

        // Create an ItemGroup for AndroidEnvironment files to be generated
        var itemGroup = new XElement("ItemGroup");
        foreach (var (key, value) in environmentVariables.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            itemGroup.Add(new XElement("_GeneratedAndroidEnvironment", new XAttribute("Include", $"{key}={value}")));
        }
        projectElement.Add(itemGroup);

        // Add target to generate environment file(s)
        var targetElement = new XElement(
            "Target",
            new XAttribute("Name", "AspireGenerateAndroidEnvironmentFiles"),
            new XAttribute("BeforeTargets", "_GenerateEnvironmentFiles"),
            new XAttribute("Condition", "'@(_GeneratedAndroidEnvironment)' != ''")
        );

        // Write environment variables to a temporary file in IntermediateOutputPath
        targetElement.Add(new XElement(
            "WriteLinesToFile",
            new XAttribute("File", "$(IntermediateOutputPath)__aspire_environment__.txt"),
            new XAttribute("Lines", "@(_GeneratedAndroidEnvironment)"),
            new XAttribute("Overwrite", "True"),
            new XAttribute("WriteOnlyWhenDifferent", "True")
        ));

        // Add the file to AndroidEnvironment items
        targetElement.Add(new XElement(
            "ItemGroup",
            new XElement("AndroidEnvironment", new XAttribute("Include", "$(IntermediateOutputPath)__aspire_environment__.txt"))
        ));

        // Add the file to FileWrites for clean
        targetElement.Add(new XElement(
            "ItemGroup",
            new XElement("FileWrites", new XAttribute("Include", "$(IntermediateOutputPath)__aspire_environment__.txt"))
        ));

        // Force the GeneratePackageManagerJava target to re-run by deleting its stamp file
        targetElement.Add(new XElement(
            "Delete",
            new XAttribute("Files", "$(_AndroidStampDirectory)_GeneratePackageManagerJava.stamp")
        ));

        projectElement.Add(targetElement);

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), projectElement);

        using var stringWriter = new StringWriter();
        document.Save(stringWriter);
        return stringWriter.ToString();
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
                logger.LogDebug(ex, "Failed to prune stale Android environment targets file '{TargetsFile}'.", file);
            }
        }

        if (deletedFiles.Count > 0)
        {
            logger.LogDebug("Pruned {Count} stale Android environment targets file(s): {Files}", deletedFiles.Count, string.Join(", ", deletedFiles));
        }
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
