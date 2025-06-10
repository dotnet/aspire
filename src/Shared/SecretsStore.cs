// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// Adapted from dotnet user-secrets at https://github.com/dotnet/aspnetcore/blob/482730a4c773ee4b3ae9525186d10999c89b556d/src/Tools/dotnet-user-secrets/src/Internal/SecretsStore.cs
/// </summary>
internal sealed class SecretsStore
{
    private readonly string _secretsFilePath;
    private readonly Dictionary<string, string?> _secrets;

    public SecretsStore(string userSecretsId)
    {
        ArgumentNullException.ThrowIfNull(userSecretsId);

        _secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        EnsureUserSecretsDirectory();

        _secrets = Load(_secretsFilePath);
    }

    public string? this[string key] => _secrets[key];

    public int Count => _secrets.Count;

    // For testing.
    internal string SecretsFilePath => _secretsFilePath;

    public bool ContainsKey(string key) => _secrets.ContainsKey(key);

    public IEnumerable<KeyValuePair<string, string?>> AsEnumerable() => _secrets;

    public void Clear() => _secrets.Clear();

    public void Set(string key, string value) => _secrets[key] = value;

    public bool Remove(string key) => _secrets.Remove(key);

    public void Save()
    {
        EnsureUserSecretsDirectory();

        var contents = new JsonObject();
        if (_secrets is not null)
        {
            foreach (var secret in _secrets.AsEnumerable())
            {
                contents[secret.Key] = secret.Value;
            }
        }

        // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
        if (!OperatingSystem.IsWindows())
        {
            var tempFilename = Path.GetTempFileName();
            File.Move(tempFilename, _secretsFilePath, overwrite: true);
        }

        var json = contents.ToJsonString(new()
        {
            WriteIndented = true
        });

        File.WriteAllText(_secretsFilePath, json, Encoding.UTF8);
    }

    private void EnsureUserSecretsDirectory()
    {
        var directoryName = Path.GetDirectoryName(_secretsFilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }

    private static Dictionary<string, string?> Load(string secretsFilePath)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(secretsFilePath, optional: true)
            .Build()
            .AsEnumerable()
            .Where(i => i.Value != null)
            .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets a value in user secrets for the project associated with the given assembly if it's not already present in configuration.
    /// </summary>
    public static void GetOrSetUserSecret(IConfigurationManager configuration, Assembly? appHostAssembly, string name, Func<string> valueGenerator)
    {
        var existingValue = configuration[name];
        if (existingValue is null)
        {
            var value = valueGenerator();
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [name] = value
                }
            );
            if (!TrySetUserSecret(appHostAssembly, name, value))
            {
                // This is a best-effort operation, so we don't throw if it fails. Common reason for failure is that the user secrets ID is not set
                // in the application's assembly. Note there's no ILogger available this early in the application lifecycle.
                Debug.WriteLine($"Failed to save value to application user secrets.");
            }
        }
    }

    /// <summary>
    /// Attempts to save a user secret value for the project associated with the given assembly. Returns a boolean indicating
    /// success or failure. If the assembly does not have a <see cref="UserSecretsIdAttribute"/>, or if the user secrets store
    /// save operation fails, this method will return false.
    /// </summary>
    public static bool TrySetUserSecret(Assembly? assembly, string name, string value)
    {
        if (assembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is { } userSecretsId)
        {
            // Save the value to the secret store
            try
            {
                var secretsStore = new SecretsStore(userSecretsId);
                secretsStore.Set(name, value);
                secretsStore.Save();
                return true;
            }
            catch (Exception) { } // Ignore user secret store errors
        }

        return false;
    }
}
