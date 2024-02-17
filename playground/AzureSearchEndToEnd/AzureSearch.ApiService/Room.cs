// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AzureSearch.ApiService;

public partial class Room
{
    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
    public string? Description { get; set; }

    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.FrMicrosoft)]
    [JsonPropertyName("Description_fr")]
    public string? DescriptionFr { get; set; }

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string? Type { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public double? BaseRate { get; set; }

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string? BedOptions { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public int SleepsCount { get; set; }

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public bool? SmokingAllowed { get; set; }

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string[]? Tags { get; set; }
}
