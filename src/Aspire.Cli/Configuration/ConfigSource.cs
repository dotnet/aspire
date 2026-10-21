// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Configuration;

internal sealed record ConfigSource(
    [property: JsonPropertyName("repository")] string Repository,
    [property: JsonPropertyName("path")] string Path)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Repository) || string.IsNullOrWhiteSpace(Path))
        {
            throw new ConfigSourceException(ConfigSourceStrings.InvalidBinding);
        }
        // Git accepts https://host/repo, ssh://git@host/repo, and git@host:repo.
        // Do not accept local paths, remote helpers, or URL credentials that would be persisted.
        var isUrl = Uri.TryCreate(Repository, UriKind.Absolute, out var uri)
            && uri.Scheme is "https" or "ssh"
            && !string.IsNullOrEmpty(uri.Host)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && (uri.Scheme == "ssh" ? !uri.UserInfo.Contains(':') : string.IsNullOrEmpty(uri.UserInfo));
        var at = Repository.IndexOf('@');
        var colon = Repository.IndexOf(':');
        var isScp = at > 0 && colon > at + 1 && colon < Repository.Length - 1
            && !Repository.Contains("://", StringComparison.Ordinal)
            && !Repository[..colon].Contains('/')
            && !Repository.Contains('\\')
            && Repository[0] != '-'
            && Repository[..at].All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')
            && Uri.CheckHostName(Repository[(at + 1)..colon]) != UriHostNameType.Unknown;
        if (Repository.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))
            || Repository.Contains('?') || Repository.Contains('#')
            || Repository.StartsWith('-') || (!isUrl && !isScp))
        {
            throw new ConfigSourceException(ConfigSourceStrings.InvalidRepository);
        }

        if (Path.Contains('\\') || Path.Contains(':')
            || Path.Any(char.IsControl)
            || Path.Split('/').Any(p => p is "" or "." or ".."))
        {
            throw new ConfigSourceException(ConfigSourceStrings.InvalidPath);
        }
    }
}

internal sealed record ConfigSourceMetadata(ConfigSource Source, string Commit, DateTimeOffset RefreshedAt);

internal sealed record ConfigSourceSnapshot(ConfigSourceMetadata Metadata, JsonObject Configuration);

internal sealed class ConfigSourceException(string message) : Exception(message);
