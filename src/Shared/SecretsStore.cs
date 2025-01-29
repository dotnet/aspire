// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// Adapted from dotnet user-secrets at https://github.com/dotnet/aspnetcore/blob/482730a4c773ee4b3ae9525186d10999c89b556d/src/Tools/dotnet-user-secrets/src/Internal/SecretsStore.cs
/// </summary>
internal abstract class KeyValueStore
{
    private readonly string _filePath;
    private readonly Dictionary<string, string?> _store;

    protected KeyValueStore(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        _filePath = filePath;

        EnsureDirectory();

        _store = Load(_filePath);
    }

    public string? this[string key] => _store[key];

    public int Count => _store.Count;

    // For testing.
    internal string FilePath => _filePath;

    public bool ContainsKey(string key) => _store.ContainsKey(key);

    public IEnumerable<KeyValuePair<string, string?>> AsEnumerable() => _store;

    public void Clear() => _store.Clear();

    public void Set(string key, string value) => _store[key] = value;

    public bool Remove(string key) => _store.Remove(key);

    public void Save()
    {
        EnsureDirectory();

        var contents = new JsonObject();
        if (_store is not null)
        {
            foreach (var item in _store.AsEnumerable())
            {
                contents[item.Key] = item.Value;
            }
        }

        // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
        if (!OperatingSystem.IsWindows())
        {
            var tempFilename = Path.GetTempFileName();
            File.Move(tempFilename, _filePath, overwrite: true);
        }

        var json = contents.ToJsonString(new()
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json, Encoding.UTF8);
    }

    protected virtual void EnsureDirectory()
    {
        var directoryName = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }

    private static Dictionary<string, string?> Load(string filePath)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(filePath, optional: true)
            .Build()
            .AsEnumerable()
            .Where(i => i.Value != null)
            .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed class SecretsStore : KeyValueStore
{
    public SecretsStore(string userSecretsId)
        : base(PathHelper.GetSecretsPathFromSecretsId(userSecretsId))
    {
        ArgumentNullException.ThrowIfNull(userSecretsId);
    }
}
