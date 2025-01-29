// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Cryptography;
using IdentityModel;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting.Utils;

internal sealed class AspireStore : KeyValueStore
{
    private readonly string _storeBasePath;
    private const string StoreFileName = "aspire.json";
    private static readonly SearchValues<char> s_invalidChars = SearchValues.Create(['/', '\\', '?', '%', '*', ':', '|', '"', '<', '>', '.', ' ']);

    private AspireStore(string basePath)
        : base(Path.Combine(basePath, StoreFileName))
    {
        ArgumentNullException.ThrowIfNull(basePath);

        _storeBasePath = basePath;
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

    protected override void EnsureDirectory()
    {
        var directoryName = Path.GetDirectoryName(FilePath);
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

    public string GetOrCreateFileWithContent(string filename, Stream contentStream)
    {
        // THIS HASN'T BEEN TESTED YET. FOR DISCUSSIONS ONLY.

        ArgumentNullException.ThrowIfNullOrWhiteSpace(filename);
        ArgumentNullException.ThrowIfNull(contentStream);

        EnsureDirectory();

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
        EnsureDirectory();

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

    /// <summary>
    /// Removes any unwanted characters from the <paramref name="filename"/>.
    /// </summary>
    internal static string Sanitize(string filename)
    {
        return string.Create(filename.Length, filename, static (s, name) =>
        {
            var nameSpan = name.AsSpan();

            for (var i = 0; i < nameSpan.Length; i++)
            {
                var c = nameSpan[i];

                s[i] = s_invalidChars.Contains(c) ? '_' : c;
            }
        });
    }
}
