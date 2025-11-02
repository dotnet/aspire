// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Publishing.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Aspire.Hosting.UserSecrets;

/// <summary>
/// Factory for creating and caching IUserSecretsManager instances.
/// Uses ConditionalWeakTable to allow instances to be shared across assemblies while still being GC-friendly.
/// </summary>
internal sealed class UserSecretsManagerFactory
{
    // Singleton instance
    public static readonly UserSecretsManagerFactory Instance = new();

    // Use ConditionalWeakTable to cache instances by file path
    // This allows the same instance to be reused while still being GC-friendly
    private readonly ConditionalWeakTable<string, IUserSecretsManager> _managerCache = new();
    
    // Semaphores are stored separately and never GC'd to ensure thread safety
    private readonly Dictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly object _semaphoresLock = new();

    private UserSecretsManagerFactory()
    {
    }

    /// <summary>
    /// Gets or creates a user secrets manager for the specified file path.
    /// </summary>
    public IUserSecretsManager GetOrCreate(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        var normalizedPath = Path.GetFullPath(filePath);
        
        return _managerCache.GetValue(normalizedPath, path =>
        {
            var semaphore = GetSemaphore(path);
            return new UserSecretsManager(path, semaphore);
        });
    }

    /// <summary>
    /// Gets or creates a user secrets manager for the specified user secrets ID.
    /// </summary>
    public IUserSecretsManager? GetOrCreateFromId(string? userSecretsId)
    {
        if (string.IsNullOrWhiteSpace(userSecretsId))
        {
            return null;
        }

        var filePath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        return GetOrCreate(filePath);
    }

    /// <summary>
    /// Gets or creates a user secrets manager for the assembly with UserSecretsIdAttribute.
    /// </summary>
    public IUserSecretsManager? GetOrCreate(Assembly? assembly)
    {
        var userSecretsId = assembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
        return GetOrCreateFromId(userSecretsId);
    }

    /// <summary>
    /// Creates a new user secrets manager for the specified file path without caching.
    /// This method is intended for testing scenarios where isolation between tests is required.
    /// </summary>
    public IUserSecretsManager Create(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        var normalizedPath = Path.GetFullPath(filePath);
        var semaphore = GetSemaphore(normalizedPath);
        return new UserSecretsManager(normalizedPath, semaphore);
    }

    /// <summary>
    /// Creates a new user secrets manager for the specified user secrets ID without caching.
    /// This method is intended for testing scenarios where isolation between tests is required.
    /// </summary>
    public IUserSecretsManager? CreateFromId(string? userSecretsId)
    {
        if (string.IsNullOrWhiteSpace(userSecretsId))
        {
            return null;
        }

        var filePath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        return Create(filePath);
    }

    /// <summary>
    /// Creates a new user secrets manager for the assembly with UserSecretsIdAttribute without caching.
    /// This method is intended for testing scenarios where isolation between tests is required.
    /// </summary>
    public IUserSecretsManager? Create(Assembly? assembly)
    {
        var userSecretsId = assembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
        return CreateFromId(userSecretsId);
    }

    private SemaphoreSlim GetSemaphore(string filePath)
    {
        lock (_semaphoresLock)
        {
            if (!_semaphores.TryGetValue(filePath, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _semaphores[filePath] = semaphore;
            }
            return semaphore;
        }
    }

    private sealed class UserSecretsManager : IUserSecretsManager
    {
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };
        private readonly SemaphoreSlim _semaphore;

        public UserSecretsManager(string filePath, SemaphoreSlim semaphore)
        {
            FilePath = filePath;
            _semaphore = semaphore;
        }

        public string FilePath { get; }

        public bool TrySetSecret(string name, string value)
        {
            try
            {
                _semaphore.Wait();
                try
                {
                    SetSecretCore(name, value);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> TrySetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await SetSecretCoreAsync(name, value, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void GetOrSetSecret(IConfigurationManager configuration, string name, Func<string> valueGenerator)
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
                if (!TrySetSecret(name, value))
                {
                    Debug.WriteLine($"Failed to save value to application user secrets.");
                }
            }
        }

        public async Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var flattenedState = JsonFlattener.FlattenJsonObject(state);
                EnsureUserSecretsDirectory();
                
                var json = flattenedState.ToJsonString(s_jsonSerializerOptions);
                await File.WriteAllTextAsync(FilePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void SetSecretCore(string name, string value)
        {
            EnsureUserSecretsDirectory();
            
            // Load existing secrets, merge with new value, save
            var secrets = Load();
            secrets[name] = value;
            Save(secrets);
        }

        private async Task SetSecretCoreAsync(string name, string value, CancellationToken cancellationToken)
        {
            EnsureUserSecretsDirectory();
            
            // Load existing secrets, merge with new value, save
            var secrets = Load();
            secrets[name] = value;
            await SaveAsync(secrets, cancellationToken).ConfigureAwait(false);
        }

        private Dictionary<string, string?> Load()
        {
            return new ConfigurationBuilder()
                .AddJsonFile(FilePath, optional: true)
                .Build()
                .AsEnumerable()
                .Where(i => i.Value != null)
                .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);
        }

        private void Save(Dictionary<string, string?> secrets)
        {
            var contents = new JsonObject();
            foreach (var secret in secrets)
            {
                contents[secret.Key] = secret.Value;
            }

            var json = contents.ToJsonString(s_jsonSerializerOptions);

            // Create a temp file with the correct Unix file mode before moving it to the expected path.
            if (!OperatingSystem.IsWindows())
            {
                var tempFilename = Path.GetTempFileName();
                File.WriteAllText(tempFilename, json, Encoding.UTF8);
                File.Move(tempFilename, FilePath, overwrite: true);
            }
            else
            {
                File.WriteAllText(FilePath, json, Encoding.UTF8);
            }
        }

        private async Task SaveAsync(Dictionary<string, string?> secrets, CancellationToken cancellationToken)
        {
            var contents = new JsonObject();
            foreach (var secret in secrets)
            {
                contents[secret.Key] = secret.Value;
            }

            var json = contents.ToJsonString(s_jsonSerializerOptions);

            // Create a temp file with the correct Unix file mode before moving it to the expected path.
            if (!OperatingSystem.IsWindows())
            {
                var tempFilename = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFilename, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                File.Move(tempFilename, FilePath, overwrite: true);
            }
            else
            {
                await File.WriteAllTextAsync(FilePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }
        }

        private void EnsureUserSecretsDirectory()
        {
            var directoryName = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
    }
}
