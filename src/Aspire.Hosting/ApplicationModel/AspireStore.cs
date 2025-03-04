// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Hashing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

internal sealed class AspireStore : IAspireStore
{
    internal const string AspireStorePathKeyName = "Aspire:Store:Path";

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

    public string GetFileNameWithContent(string filenameTemplate, Stream contentStream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filenameTemplate);
        ArgumentNullException.ThrowIfNull(contentStream);

        EnsureDirectory();

        // Strip any folder information from the filename.
        filenameTemplate = Path.GetFileName(filenameTemplate);

        // Create a temporary file to write the content to.
        var tempFileName = Path.GetTempFileName();

        // Fast, non-cryptographic hash.
        var hash = new XxHash3();

        // Write the content to the temporary file while also building a hash.
        using (var fileStream = File.OpenWrite(tempFileName))
        {
            using var digestStream = new HashDigestStream(fileStream, hash);
            contentStream.CopyTo(digestStream);
        }

        var name = Path.GetFileNameWithoutExtension(filenameTemplate);
        var ext = Path.GetExtension(filenameTemplate);
        var finalFilePath = Path.Combine(_basePath, $"{name}.{Convert.ToHexString(hash.GetCurrentHash())[..12].ToLowerInvariant()}{ext}");

        if (!File.Exists(finalFilePath))
        {
            File.Copy(tempFileName, finalFilePath, overwrite: true);
        }

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
