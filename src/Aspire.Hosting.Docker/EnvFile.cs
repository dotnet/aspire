// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

internal sealed record EnvEntry(string Key, string? Value, string? Comment);

internal sealed class EnvFile
{
    private string? _path;

    internal SortedDictionary<string, EnvEntry> Entries { get; } = [];

    public static EnvFile Load(string path)
    {
        var envFile = new EnvFile { _path = path };
        if (!File.Exists(path))
        {
            return envFile;
        }

        string? currentComment = null;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                // Extract comment text (remove # and trim)
                currentComment = trimmed.Length > 1 ? trimmed[1..].Trim() : string.Empty;
            }
            else if (TryParseKeyValue(line, out var key, out var value))
            {
                envFile.Entries[key] = new EnvEntry(key, value, currentComment);
                currentComment = null; // Reset comment after associating it with a key
            }
            else
            {
                // Reset comment if we encounter a non-comment, non-key line
                currentComment = null;
            }
        }
        return envFile;
    }

    public void Add(string key, string? value, string? comment, bool onlyIfMissing = true)
    {
        if (Entries.ContainsKey(key) && onlyIfMissing)
        {
            return;
        }

        Entries[key] = new EnvEntry(key, value, comment);
    }

    private static bool TryParseKeyValue(string line, out string key, out string? value)
    {
        key = string.Empty;
        value = null;
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith('#') && trimmed.Contains('='))
        {
            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex > 0)
            {
                key = trimmed[..eqIndex].Trim();
                value = eqIndex < trimmed.Length - 1 ? trimmed[(eqIndex + 1)..] : string.Empty;
                return true;
            }
        }
        return false;
    }

    public void Save()
    {
        if (_path is null)
        {
            throw new InvalidOperationException("Cannot save EnvFile without a path. Use Load() to create an EnvFile with a path.");
        }

        var lines = new List<string>();

        foreach (var entry in Entries.Values)
        {
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                lines.Add($"# {entry.Comment}");
            }
            lines.Add(entry.Value is not null ? $"{entry.Key}={entry.Value}" : $"{entry.Key}=");
            lines.Add(string.Empty);
        }

        File.WriteAllLines(_path, lines);
    }

    public void Save(bool includeValues)
    {
        if (includeValues)
        {
            Save();
        }
        else
        {
            SaveKeysOnly();
        }
    }

    private void SaveKeysOnly()
    {
        if (_path is null)
        {
            throw new InvalidOperationException("Cannot save EnvFile without a path. Use Load() to create an EnvFile with a path.");
        }

        var lines = new List<string>();

        foreach (var entry in Entries.Values)
        {
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                lines.Add($"# {entry.Comment}");
            }

            // If the entry already has a non-empty value (loaded from disk), preserve it
            // This ensures user-modified values are not overwritten when we save keys only
            if (!string.IsNullOrEmpty(entry.Value))
            {
                lines.Add($"{entry.Key}={entry.Value}");
            }
            else
            {
                lines.Add($"{entry.Key}=");
            }
            
            lines.Add(string.Empty);
        }

        File.WriteAllLines(_path, lines);
    }
}
