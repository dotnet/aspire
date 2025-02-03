// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection;
using System.Security.Cryptography;

namespace Aspire.Hosting.Utils;

internal sealed class AspireStore
{
    internal const string AspireStoreDir = "ASPIRE_STORE_DIR";

    private readonly string _basePath;
    private static readonly SearchValues<char> s_invalidFileNameChars = SearchValues.Create(Path.GetInvalidFileNameChars());

    private AspireStore(string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        _basePath = basePath;
        EnsureDirectory();
    }

    internal string BasePath => _basePath;

    /// <summary>
    /// Creates a new instance of <see cref="AspireStore"/> using the provided <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>A new instance of <see cref="AspireStore"/>.</returns>
    /// <remarks>
    /// The store is created in the ./obj folder of the Application Host.
    /// If the ASPIRE_STORE_DIR environment variable is set this will be used instead.
    /// </remarks>
    public static AspireStore Create(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var assemblyMetadata = builder.AppHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        var objDir = GetMetadataValue(assemblyMetadata, "AppHostProjectBaseIntermediateOutputPath");

        var fallbackDir = Environment.GetEnvironmentVariable(AspireStoreDir);
        var root = fallbackDir ?? objDir;

        if (string.IsNullOrEmpty(root))
        {
            throw new InvalidOperationException($"Could not determine an appropriate location for storing user secrets. Set the {AspireStoreDir} environment variable to a folder where the App Host content should be stored.");
        }

        var directoryPath = Path.Combine(root, ".aspire");

        // The /obj directory doesn't need to be prefixed with the app host name.
        if (root != objDir)
        {
            directoryPath = Path.Combine(directoryPath, GetAppHostSpecificPrefix(builder));
        }

        return new AspireStore(directoryPath);
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key) =>
        assemblyMetadata?.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

    private static string GetAppHostSpecificPrefix(IDistributedApplicationBuilder builder)
    {
        var appName = Sanitize(builder.Environment.ApplicationName).ToLowerInvariant();
        var appNameHash = builder.Configuration["AppHost:Sha256"]![..10].ToLowerInvariant();
        return $"{appName}.{appNameHash}";
    }

    /// <summary>
    /// Gets a deterministic file path that is a copy of the <paramref name="sourceFilename"/>.
    /// The resulting file name will depend on the content of the file.
    /// </summary>
    /// <param name="filename">A file name the to base the result on.</param>
    /// <param name="sourceFilename">An existing file.</param>
    /// <returns>A deterministic file path with the same content as <paramref name="sourceFilename"/>.</returns>
    public string GetFileNameWithContent(string filename, string sourceFilename)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(filename);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(sourceFilename);

        if (!File.Exists(sourceFilename))
        {
            throw new FileNotFoundException("The source file '{0}' does not exist.", sourceFilename);
        }

        EnsureDirectory();

        // Strip any folder information from the filename.
        filename = Path.GetFileName(filename);

        // Delete existing file versions with the same name.
        var allFiles = Directory.EnumerateFiles(_basePath, filename + ".*");

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

        var hashStream = File.OpenRead(sourceFilename);

        // Compute the hash of the content.
        var hash = SHA256.HashData(hashStream);

        hashStream.Dispose();

        var name = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename);
        var finalFilePath = Path.Combine(_basePath, $"{name}.{Convert.ToHexString(hash)[..12].ToLowerInvariant()}{ext}".ToLowerInvariant());

        if (!File.Exists(finalFilePath))
        {
            File.Copy(sourceFilename, finalFilePath, overwrite: true);
        }

        return finalFilePath;
    }

    public string GetFileNameWithContent(string filename, Stream contentStream)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(filename);
        ArgumentNullException.ThrowIfNull(contentStream);

        // Create a temporary file to write the content to.
        var tempFileName = Path.GetTempFileName();

        // Write the content to the temporary file.
        using (var fileStream = File.OpenWrite(tempFileName))
        {
            contentStream.CopyTo(fileStream);
        }

        var finalFilePath = GetFileNameWithContent(filename, tempFileName);

        File.Delete(tempFileName);

        return finalFilePath;
    }

    /// <summary>
    /// Creates a file with the provided <paramref name="filename"/> in the store.
    /// </summary>
    /// <param name="filename">The file name to use in the store.</param>
    /// <returns>The absolute file name in the store.</returns>
    public string GetFileName(string filename)
    {
        EnsureDirectory();

        // Strip any folder information from the filename.
        filename = Path.GetFileName(filename);

        return Path.Combine(_basePath, filename);
    }

    public void DeleteFile(string filename)
    {
        // Strip any folder information from the filename.
        filename = Path.GetFileName(filename);

        var finalFilePath = Path.Combine(_basePath, filename);

        if (File.Exists(finalFilePath))
        {
            File.Delete(finalFilePath);
        }
    }

    public void DeleteStore()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, recursive: true);
        }
    }

    /// <summary>
    /// Removes any unwanted characters from the <paramref name="filename"/>.
    /// </summary>
    internal static string Sanitize(string filename)
    {
        return string.Create(filename.Length, filename, static (s, name) =>
        {
            name.CopyTo(s);

            while (s.IndexOfAny(s_invalidFileNameChars) is var i and not -1)
            {
                s[i] = '_';
            }
        });
    }

    private void EnsureDirectory()
    {
        if (!string.IsNullOrEmpty(_basePath) && !Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }
}
