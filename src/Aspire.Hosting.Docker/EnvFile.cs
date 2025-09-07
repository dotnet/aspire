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

    public void AddIfMissing(string key, string? value, string? comment)
    {
        if (_keys.Contains(key))
        {
            return;
        }
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
