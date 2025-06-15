// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
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

    /// <summary>
    /// Sets values from a JsonObject, flattening nested structures to use colon-separated keys for configuration compatibility.
    /// This ensures all secrets are stored in the flat format expected by .NET configuration.
    /// </summary>
    public void SetFromJsonObject(JsonObject jsonObject)
    {
        var flattened = FlattenJsonObject(jsonObject);
        foreach (var kvp in flattened)
        {
            if (kvp.Value is not null)
            {
                Set(kvp.Key, kvp.Value.ToString());
            }
        }
    }

    /// <summary>
    /// Flattens a JsonObject to use colon-separated keys for configuration compatibility.
    /// This ensures all secrets are stored in the flat format expected by .NET configuration.
    /// </summary>
    internal static JsonObject FlattenJsonObject(JsonObject source)
    {
        var result = new JsonObject();
        FlattenJsonObjectRecursive(source, string.Empty, result);
        return result;
    }

    private static void FlattenJsonObjectRecursive(JsonObject source, string prefix, JsonObject result)
    {
        foreach (var kvp in source)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";
            
            if (kvp.Value is JsonObject nestedObject)
            {
                FlattenJsonObjectRecursive(nestedObject, key, result);
            }
            else if (kvp.Value is JsonArray array)
            {
                // Flatten arrays using index-based keys (standard .NET configuration format)
                for (int i = 0; i < array.Count; i++)
                {
                    var arrayKey = $"{key}:{i}";
                    if (array[i] is JsonObject arrayObject)
                    {
                        FlattenJsonObjectRecursive(arrayObject, arrayKey, result);
                    }
                    else
                    {
                        result[arrayKey] = array[i]?.DeepClone();
                    }
                }
            }
            else
            {
                result[key] = kvp.Value?.DeepClone();
            }
        }
    }

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
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
