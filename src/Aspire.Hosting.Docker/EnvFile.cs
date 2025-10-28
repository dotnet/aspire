// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

internal sealed class EnvFile
{
    private readonly List<string> _lines = [];
    private readonly HashSet<string> _keys = [];

    public static EnvFile Load(string path)
    {
        var envFile = new EnvFile();
        if (!File.Exists(path))
        {
            return envFile;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            envFile._lines.Add(line);
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith('#') && trimmed.Contains('='))
            {
                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = trimmed[..eqIndex].Trim();
                    envFile._keys.Add(key);
                }
            }
        }
        return envFile;
    }

    public void Add(string key, string? value, string? comment, bool onlyIfMissing = true)
    {
        if (_keys.Contains(key))
        {
            if (onlyIfMissing)
            {
                return;
            }

            // If the key already exists and we want to update it (onlyIfMissing = false),
            // we need to find and replace the existing entry
            // Find the existing key-value line and replace it
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i].TrimStart();
                if (!line.StartsWith('#') && line.Contains('='))
                {
                    var eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var existingKey = line[..eqIndex].Trim();
                        if (existingKey == key)
                        {
                            _lines[i] = value is not null ? $"{key}={value}" : $"{key}=";
                            return;
                        }
                    }
                }
            }
        }

        // Add new entry
        if (!string.IsNullOrWhiteSpace(comment))
        {
            _lines.Add($"# {comment}");
        }
        _lines.Add(value is not null ? $"{key}={value}" : $"{key}=");
        _lines.Add(string.Empty);
        _keys.Add(key);
    }

    public void Save(string path)
    {
        File.WriteAllLines(path, _lines);
    }
}
