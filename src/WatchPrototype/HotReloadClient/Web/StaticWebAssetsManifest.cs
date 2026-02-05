// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

internal sealed class StaticWebAssetsManifest(ImmutableDictionary<string, string> urlToPathMap, ImmutableArray<StaticWebAssetPattern> discoveryPatterns)
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        RespectNullableAnnotations = true,
    };

    private sealed class ManifestJson
    {
        public required List<string> ContentRoots { get; init; }
        public required ChildAssetJson Root { get; init; }
    }

    private sealed class ChildAssetJson
    {
        public Dictionary<string, ChildAssetJson>? Children { get; init; }
        public AssetInfoJson? Asset { get; init; }
        public List<PatternJson>? Patterns { get; init; }
    }

    private sealed class AssetInfoJson
    {
        public required int ContentRootIndex { get; init; }
        public required string SubPath { get; init; }
    }

    private sealed class PatternJson
    {
        public required int ContentRootIndex { get; init; }
        public required string Pattern { get; init; }
    }

    /// <summary>
    /// Maps relative URLs to file system paths.
    /// </summary>
    public ImmutableDictionary<string, string> UrlToPathMap { get; } = urlToPathMap;

    /// <summary>
    /// List of directory and search pattern pairs for discovering static web assets.
    /// </summary>
    public ImmutableArray<StaticWebAssetPattern> DiscoveryPatterns { get; } = discoveryPatterns;

    public bool TryGetBundleFilePath(string bundleFileName, [NotNullWhen(true)] out string? filePath)
    {
        if (UrlToPathMap.TryGetValue(bundleFileName, out var bundleFilePath))
        {
            filePath = bundleFilePath;
            return true;
        }

        foreach (var entry in UrlToPathMap)
        {
            var url = entry.Key;
            var path = entry.Value;

            if (Path.GetFileName(path).Equals(bundleFileName, StringComparison.Ordinal))
            {
                filePath = path;
                return true;
            }
        }

        filePath = null;
        return false;
    }

    public static StaticWebAssetsManifest? TryParseFile(string path, ILogger logger)
    {
        Stream? stream;

        logger.LogDebug("Reading static web assets manifest file: '{FilePath}'.", path);

        try
        {
            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to read '{FilePath}': {Message}", path, e.Message);
            return null;
        }

        try
        {
            return TryParse(stream, path, logger);
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <exception cref="JsonException">The format is invalid.</exception>
    public static StaticWebAssetsManifest? TryParse(Stream stream, string filePath, ILogger logger)
    {
        ManifestJson? manifest;

        try
        {
            manifest = JsonSerializer.Deserialize<ManifestJson>(stream, s_options);
        }
        catch (JsonException e)
        {
            logger.LogError("Failed to parse '{FilePath}': {Message}", filePath, e.Message);
            return null;
        }

        if (manifest == null)
        {
            logger.LogError("Failed to parse '{FilePath}'", filePath);
            return null;
        }

        var validContentRoots = new string?[manifest.ContentRoots.Count];

        for (var i = 0; i < validContentRoots.Length; i++)
        {
            var root = manifest.ContentRoots[i];
            if (Path.IsPathFullyQualified(root))
            {
                validContentRoots[i] = root;
            }
            else
            {
                logger.LogWarning("Failed to parse '{FilePath}': ContentRoots path not fully qualified: {Root}", filePath, root);
            }
        }

        var urlToPathMap = ImmutableDictionary.CreateBuilder<string, string>();
        var discoveryPatterns = ImmutableArray.CreateBuilder<StaticWebAssetPattern>();

        ProcessNode(manifest.Root, url: "");

        return new StaticWebAssetsManifest(urlToPathMap.ToImmutable(), discoveryPatterns.ToImmutable());

        void ProcessNode(ChildAssetJson node, string url)
        {
            if (node.Children != null)
            {
                foreach (var entry in node.Children)
                {
                    var childName = entry.Key;
                    var child = entry.Value;

                    ProcessNode(child, url: (url is []) ? childName : url + "/" + childName);
                }
            }

            if (node.Asset != null)
            {
                if (url == "")
                {
                    logger.LogWarning("Failed to parse '{FilePath}': Asset has no URL", filePath);
                    return;
                }

                if (!TryGetContentRoot(node.Asset.ContentRootIndex, out var root))
                {
                    return;
                }

                urlToPathMap[url] = Path.Join(root, node.Asset.SubPath.Replace('/', Path.DirectorySeparatorChar));
            }
            else if (node.Children == null)
            {
                logger.LogWarning("Failed to parse '{FilePath}': Missing Asset", filePath);
            }

            if (node.Patterns != null)
            {
                foreach (var pattern in node.Patterns)
                {
                    if (TryGetContentRoot(pattern.ContentRootIndex, out var root))
                    {
                        discoveryPatterns.Add(new StaticWebAssetPattern(root, pattern.Pattern, url));
                    }
                }
            }

            bool TryGetContentRoot(int index, [NotNullWhen(true)] out string? contentRoot)
            {
                if (index < 0 || index >= validContentRoots.Length)
                {
                    logger.LogWarning("Failed to parse '{FilePath}': Invalid value of ContentRootIndex: {Value}", filePath, index);
                    contentRoot = null;
                    return false;
                }

                contentRoot = validContentRoots[index];
                return contentRoot != null;
            }
        }
    }
}
