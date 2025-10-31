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
        var semaphore = UserSecretsFileLock.GetSemaphore(_secretsFilePath);
        semaphore.Wait();
        try
        {
            // Reload from disk to merge with any concurrent changes
            var currentSecrets = Load(_secretsFilePath);
            
            // Merge our changes with what's on disk
            foreach (var kvp in _secrets)
            {
                currentSecrets[kvp.Key] = kvp.Value;
            }

            EnsureUserSecretsDirectory();

            var contents = new JsonObject();
            foreach (var secret in currentSecrets)
            {
                contents[secret.Key] = secret.Value;
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
        finally
        {
            semaphore.Release();
        }
    }

    private void EnsureUserSecretsDirectory()
    {
        EnsureUserSecretsDirectory(_secretsFilePath);
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
                var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
                var semaphore = UserSecretsFileLock.GetSemaphore(secretsFilePath);
                semaphore.Wait();
                try
                {
                    // Load, set, and save in one atomic operation to ensure thread safety
                    EnsureUserSecretsDirectory(secretsFilePath);
                    
                    var secrets = Load(secretsFilePath);
                    secrets[name] = value;

                    var contents = new JsonObject();
                    foreach (var secret in secrets)
                    {
                        contents[secret.Key] = secret.Value;
                    }

                    // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
                    if (!OperatingSystem.IsWindows())
                    {
                        var tempFilename = Path.GetTempFileName();
                        File.Move(tempFilename, secretsFilePath, overwrite: true);
                    }

                    var json = contents.ToJsonString(new()
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(secretsFilePath, json, Encoding.UTF8);
                    return true;
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (Exception) { } // Ignore user secret store errors
        }

        return false;
    }

    private static void EnsureUserSecretsDirectory(string secretsFilePath)
    {
        var directoryName = Path.GetDirectoryName(secretsFilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }
}
