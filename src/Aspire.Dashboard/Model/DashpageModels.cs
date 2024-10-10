// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using static Aspire.Dashboard.Components.Pages.Metrics;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Data about a highlight.
/// </summary>
public sealed class HighlightDefinition
{
    /// <summary>
    /// Gets the unique name of this highlight.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of this highlight. Not guaranteed to be unique.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the relative priority of this highlight, controlling ordering of highlights in UI.
    /// </summary>
    /// <remarks>
    /// Higher priorities appear higher in lists.
    /// When priorities are equal, highlights are ordered by name.
    /// </remarks>
    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("charts")]
    public required List<HighlightChartDefinition> Charts { get; init; }
}

/// <summary>
/// Data about a chart on a highlight.
/// </summary>
public sealed class HighlightChartDefinition
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("instrument")]
    public required string InstrumentName { get; init; }

    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsRequired { get; init; }

    [JsonPropertyName("resource")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceName { get; init; }

    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MetricViewKind DefaultViewKind { get; init; } = MetricViewKind.Graph;
}

public interface IHighlightPersistence
{
    Task<ImmutableArray<HighlightDefinition>> GetHighlightsAsync(CancellationToken token);
}

public sealed class HighlightJsonFilePersistence(IFileProvider fileProvider) : IHighlightPersistence
{
    private static readonly JsonSerializerOptions s_options = new() { ReadCommentHandling = JsonCommentHandling.Skip };

    // Cache file to prevent re-reading on every request.
    // In case of an exception, no caching is done.
    private ImmutableArray<HighlightDefinition> _highlights;

    public async Task<ImmutableArray<HighlightDefinition>> GetHighlightsAsync(CancellationToken token)
    {
        if (_highlights.IsDefault)
        {
            var fileInfo = fileProvider.GetFileInfo("highlights.json");

            if (fileInfo.Exists == false)
            {
                _highlights = [];
            }
            else
            {
                using Stream stream = fileInfo.CreateReadStream();

                using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

                var json = await reader.ReadToEndAsync(token).ConfigureAwait(false);

                _highlights = Deserialize(json);
            }
        }

        return _highlights;
    }

    internal static ImmutableArray<HighlightDefinition> Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ImmutableArray<HighlightDefinition>>(json, s_options);
    }
}
