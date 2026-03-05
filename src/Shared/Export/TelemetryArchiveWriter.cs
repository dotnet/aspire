// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Aspire.Shared.Export;

/// <summary>
/// Shared utility for writing telemetry and resource data to a zip archive.
/// Used by both the Dashboard export and the CLI export command.
/// </summary>
internal static class TelemetryArchiveWriter
{
    /// <summary>
    /// Sanitizes a file name by replacing invalid characters with underscores.
    /// </summary>
    internal static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            sanitized.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return sanitized.ToString();
    }

    /// <summary>
    /// Serializes data as JSON and writes it to a zip archive entry.
    /// </summary>
    internal static void WriteJsonToArchive<T>(ZipArchive archive, string path, T data, JsonTypeInfo<T> jsonTypeInfo)
    {
        var entry = archive.CreateEntry(path);
        using var entryStream = entry.Open();
        JsonSerializer.Serialize(entryStream, data, jsonTypeInfo);
    }

    /// <summary>
    /// Serializes data as JSON and writes it to a zip archive entry using serializer options.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation.")]
    internal static void WriteJsonToArchive<T>(ZipArchive archive, string path, T data, JsonSerializerOptions options)
    {
        var entry = archive.CreateEntry(path);
        using var entryStream = entry.Open();
        JsonSerializer.Serialize(entryStream, data, options);
    }

    /// <summary>
    /// Writes UTF-8 text content to a zip archive entry.
    /// </summary>
    internal static void WriteTextToArchive(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        writer.Write(content);
    }

    /// <summary>
    /// Writes console log lines to a zip archive as plain text, one line per entry.
    /// </summary>
    internal static void WriteConsoleLogsToArchive(ZipArchive archive, string resourceName, IList<string> logLines)
    {
        var entry = archive.CreateEntry($"consolelogs/{SanitizeFileName(resourceName)}.txt");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);

        foreach (var line in logLines)
        {
            writer.WriteLine(line);
        }

        writer.Flush();
    }
}
