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
/// Data about a dashpage.
/// </summary>
public sealed class DashpageDefinition
{
    /// <summary>
    /// Gets the unique name of this dashpage.
    /// </summary>
    /// <remarks>
    /// Also used as a display name.
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the relative priority of this dashpage, controlling ordering of dashpages in UI.
    /// </summary>
    /// <remarks>
    /// Higher priorities appear higher in lists.
    /// When priorities are equal, dashpages are ordered by name.
    /// </remarks>
    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("charts")]
    public required List<DashpageChartDefinition> Charts { get; init; }
}

/// <summary>
/// Data about a chart on a dashpage.
/// </summary>
public sealed class DashpageChartDefinition
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

public interface IDashpagePersistence
{
    Task<ImmutableArray<DashpageDefinition>> GetDashpagesAsync(CancellationToken token);
}

public sealed class DashpageJsonFilePersistence(IFileProvider fileProvider) : IDashpagePersistence
{
    private static readonly JsonSerializerOptions s_options = new() { ReadCommentHandling = JsonCommentHandling.Skip };

    // Cache file to prevent re-reading on every request.
    // In case of an exception, no caching is done.
    private ImmutableArray<DashpageDefinition> _dashpages;

    public async Task<ImmutableArray<DashpageDefinition>> GetDashpagesAsync(CancellationToken token)
    {
        if (_dashpages.IsDefault)
        {
            var fileInfo = fileProvider.GetFileInfo("dashpages.json");

            if (fileInfo.Exists == false)
            {
                _dashpages = [];
            }
            else
            {
                using Stream stream = fileInfo.CreateReadStream();

                using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

                var json = await reader.ReadToEndAsync(token).ConfigureAwait(false);

                _dashpages = Deserialize(json);
            }
        }

        return _dashpages;
    }

    internal static ImmutableArray<DashpageDefinition> Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ImmutableArray<DashpageDefinition>>(json, s_options);
    }
}
