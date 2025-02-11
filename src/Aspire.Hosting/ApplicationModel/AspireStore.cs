// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Aspire.Hosting.ApplicationModel;

internal sealed class AspireStore : IAspireStore
{
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspireStore"/> class with the specified base path.
    /// </summary>
    /// <param name="basePath">The base path for the store.</param>
    /// <returns>A new instance of <see cref="AspireStore"/>.</returns>
    public AspireStore(string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        if (!Path.IsPathRooted(basePath))
        {
            throw new ArgumentException($"An absolute path is required: '${basePath}'", nameof(basePath));
        }

        _basePath = basePath;
        EnsureDirectory();
    }

    public string BasePath => _basePath;

    public string GetFileNameWithContent(string filenameTemplate, string sourceFilename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filenameTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilename);

        if (!File.Exists(sourceFilename))
        {
            throw new FileNotFoundException("The source file '{0}' does not exist.", sourceFilename);
        }

        EnsureDirectory();

        // Strip any folder information from the filename.
        filenameTemplate = Path.GetFileName(filenameTemplate);

        var hashStream = File.OpenRead(sourceFilename);

        // Compute the hash of the content.
        var hash = SHA256.HashData(hashStream);

        hashStream.Dispose();

        var name = Path.GetFileNameWithoutExtension(filenameTemplate);
        var ext = Path.GetExtension(filenameTemplate);
        var finalFilePath = Path.Combine(_basePath, $"{name}.{Convert.ToHexString(hash)[..12].ToLowerInvariant()}{ext}");

        if (!File.Exists(finalFilePath))
        {
            File.Copy(sourceFilename, finalFilePath, overwrite: true);
        }

        return finalFilePath;
    }

    public string GetFileNameWithContent(string filenameTemplate, Stream contentStream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filenameTemplate);
        ArgumentNullException.ThrowIfNull(contentStream);

        // Create a temporary file to write the content to.
        var tempFileName = Path.GetTempFileName();

        // Write the content to the temporary file.
        using (var fileStream = File.OpenWrite(tempFileName))
        {
            contentStream.CopyTo(fileStream);
        }

        var finalFilePath = GetFileNameWithContent(filenameTemplate, tempFileName);

        try
        {
            File.Delete(tempFileName);
        }
        catch
        {
        }

        return finalFilePath;
    }

    /// <summary>
    /// Ensures that the directory for the store exists.
    /// </summary>
    private void EnsureDirectory()
    {
        if (!string.IsNullOrEmpty(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }
}
