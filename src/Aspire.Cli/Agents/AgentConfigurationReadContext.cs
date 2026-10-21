// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

internal sealed class AgentConfigurationReadContext
{
    private readonly Dictionary<string, byte[]?> _files = new(AgentPath.Comparer);
    private readonly Dictionary<string, (bool Exists, string[] Files)> _directories = new(AgentPath.Comparer);

    public async Task<byte[]?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        path = Path.GetFullPath(path);
        if (!_files.TryGetValue(path, out var content))
        {
            content = await ReadFileAsync(path, cancellationToken);
            _files.Add(path, content);
        }

        return content;
    }

    public IEnumerable<string> GetJsonFiles(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (!_directories.TryGetValue(directory, out var contents))
        {
            contents = ReadDirectory(directory);
            _directories.Add(directory, contents);
        }

        return contents.Files;
    }

    public async Task ValidateAsync(string targetPath, CancellationToken cancellationToken)
    {
        foreach (var (path, content) in _files)
        {
            if (!AgentPath.Comparer.Equals(path, targetPath) &&
                !Equal(content, await ReadFileAsync(path, cancellationToken)))
            {
                throw Changed(path);
            }
        }

        // Check the destination last, not before awaiting inherited settings reads.
        if (_files.TryGetValue(targetPath, out var original) &&
            !Equal(original, await ReadFileAsync(targetPath, cancellationToken)))
        {
            throw Changed(targetPath);
        }

        foreach (var (path, previous) in _directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = ReadDirectory(path);
            if (previous.Exists != current.Exists || !previous.Files.SequenceEqual(current.Files, AgentPath.Comparer))
            {
                throw Changed(path);
            }
        }
    }

    private static async Task<byte[]?> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    private static (bool Exists, string[] Files) ReadDirectory(string path)
    {
        try
        {
            return (true, Directory.EnumerateFiles(path, "*.json")
                .Where(file => !Path.GetFileName(file).StartsWith('.'))
                .Order(StringComparer.Ordinal).ToArray());
        }
        catch (DirectoryNotFoundException)
        {
            return (false, []);
        }
    }

    private static bool Equal(byte[]? left, byte[]? right)
        => left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);

    private static IOException Changed(string path)
        => new(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.ConfigurationWriteRefused, path));
}
