// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using IdentityModel;

namespace Aspire.Hosting.Utils;

internal sealed class AspireStore
{
    private readonly string _storeFilePath;
    private readonly string _storeBasePath;
    private const string StoreFileName = "aspire.json";

    private readonly Dictionary<string, string?> _store;

    private AspireStore(string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        _storeBasePath = basePath;
        _storeFilePath = Path.Combine(basePath, StoreFileName);

        EnsureStoreDirectory();

        _store = Load(_storeFilePath);
    }

    /// <summary>
    /// Creates a new instance of <see cref="AspireStore"/> using the provided <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>A new instance of <see cref="AspireStore"/>.</returns>
    /// <remarks>
    /// The store is created in the following locations:
    /// - On Windows: %APPDATA%\Aspire\{applicationHash}\aspire.json
    /// - On Mac/Linux: ~/.aspire/{applicationHash}\aspire.json
    /// - If none of the above locations are available, the store is created in the directory specified by the ASPIRE_STORE_FALLBACK_DIR environment variable.
    /// - If the ASPIRE_STORE_FALLBACK_DIR environment variable is not set, an <see cref="InvalidOperationException"/> is thrown.
    ///
    /// The directory has the permissions set to 700 on Unix systems.
    /// </remarks>
    public static AspireStore Create(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        const string aspireStoreFallbackDir = "ASPIRE_STORE_FALLBACK_DIR";

        var appData = Environment.GetEnvironmentVariable("APPDATA");
        var root = appData                                                                   // On Windows it goes to %APPDATA%\Microsoft\UserSecrets\
                   ?? Environment.GetEnvironmentVariable("HOME")                             // On Mac/Linux it goes to ~/.microsoft/usersecrets/
                   ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                   ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                   ?? Environment.GetEnvironmentVariable(aspireStoreFallbackDir);            // this fallback is an escape hatch if everything else fails

        if (string.IsNullOrEmpty(root))
        {
            throw new InvalidOperationException($"Could not determine an appropriate location for storing user secrets. Set the {aspireStoreFallbackDir} environment variable to a folder where Aspire content should be stored.");
        }

        var appName = Sanitize(builder.Environment.ApplicationName).ToLowerInvariant();
        var appNameHash = builder.Configuration["AppHost:Sha256"]![..10].ToLowerInvariant();

        var directoryPath = !string.IsNullOrEmpty(appData)
            ? Path.Combine(root, "Aspire", $"{appName}.{appNameHash}")
            : Path.Combine(root, ".aspire", $"{appName}.{appNameHash}");

        return new AspireStore(directoryPath);
    }

    public string? this[string key] => _store[key];

    public int Count => _store.Count;

    // For testing.
    internal string StoreFilePath => _storeFilePath;

    public bool ContainsKey(string key) => _store.ContainsKey(key);

    public IEnumerable<KeyValuePair<string, string?>> AsEnumerable() => _store;

    public void Clear() => _store.Clear();

    public void Set(string key, string value) => _store[key] = value;

    public bool Remove(string key) => _store.Remove(key);

    public void Save()
    {
        EnsureStoreDirectory();

        var contents = new JsonObject();
        if (_store is not null)
        {
            foreach (var secret in _store.AsEnumerable())
            {
                contents[secret.Key] = secret.Value;
            }
        }

        // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
        if (!OperatingSystem.IsWindows())
        {
            var tempFilename = Path.GetTempFileName();
            File.Move(tempFilename, _storeFilePath, overwrite: true);
        }

        var json = contents.ToJsonString(new()
        {
            WriteIndented = true
        });

        File.WriteAllText(_storeFilePath, json, Encoding.UTF8);
    }

    public string GetOrCreateFileWithContent(string filename, Stream contentStream)
    {
        // THIS HASN'T BEEN TESTED YET. FOR DISCUSSIONS ONLY.

        ArgumentNullException.ThrowIfNullOrWhiteSpace(filename);
        ArgumentNullException.ThrowIfNull(contentStream);

        EnsureStoreDirectory();

        // Strip any folder information from the filename.
        filename = Path.GetFileName(filename);

        // Delete existing file versions with the same name.
        var allFiles = Directory.EnumerateFiles(_storeBasePath, filename + ".*");

        foreach (var file in allFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }

        // Create a temporary file to write the content to.
        var tempFileName = Path.GetTempFileName();

        // Write the content to the temporary file.
        using (var fileStream = File.OpenWrite(tempFileName))
        {
            contentStream.CopyTo(fileStream);
        }

        // Compute the hash of the content.
        var hash = SHA256.HashData(File.ReadAllBytes(tempFileName));

        // Move the temporary file to the final location.
        // TODO: Use System.Buffers.Text implementation when targeting .NET 9.0 or greater
        var finalFilePath = Path.Combine(_storeBasePath, filename, ".", Base64Url.Encode(hash).ToLowerInvariant());
        File.Move(tempFileName, finalFilePath, overwrite: false);

        // If the file already exists, delete the temporary file.
        if (File.Exists(tempFileName))
        {
            File.Delete(tempFileName);
        }

        return finalFilePath;
    }

    /// <summary>
    /// Creates a file with the provided <paramref name="filename"/> if it does not exist.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public string GetOrCreateFile(string filename)
    {
        EnsureStoreDirectory();

        // Strip any folder information from the filename.
        filename = Path.GetFileName(filename);

        var finalFilePath = Path.Combine(_storeBasePath, filename);

        if (!File.Exists(finalFilePath))
        {
            var tempFileName = Path.GetTempFileName();
            File.Move(tempFileName, finalFilePath, overwrite: false);
        }

        return finalFilePath;
    }

    internal static string Sanitize(string name)
    {
        return string.Create(name.Length, name, static (s, name) =>
        {
            // According to the error message from docker CLI, volume names must be of form "[a-zA-Z0-9][a-zA-Z0-9_.-]"
            var nameSpan = name.AsSpan();

            for (var i = 0; i < nameSpan.Length; i++)
            {
                var c = nameSpan[i];

                s[i] = IsValidChar(i, c) ? c : '_';
            }
        });

        static bool IsValidChar(int i, char c)
        {
            if (i == 0 && !(char.IsAsciiLetter(c) || char.IsNumber(c)))
            {
                // First char must be a letter or number
                return false;
            }
            else if (!(char.IsAsciiLetter(c) || char.IsNumber(c) || c == '_' || c == '.' || c == '-'))
            {
                // Subsequent chars must be a letter, number, underscore, period, or hyphen
                return false;
            }

            return true;
        }
    }

    private void EnsureStoreDirectory()
    {
        var directoryName = Path.GetDirectoryName(_storeFilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            if (!OperatingSystem.IsWindows())
            {
                var tempDir = Directory.CreateTempSubdirectory();
                tempDir.MoveTo(directoryName);
            }
            else
            {
                Directory.CreateDirectory(directoryName);
            }
        }
    }

    private static Dictionary<string, string?> Load(string storeFilePath)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(storeFilePath, optional: true)
            .Build()
            .AsEnumerable()
            .Where(i => i.Value != null)
            .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);
    }
}
