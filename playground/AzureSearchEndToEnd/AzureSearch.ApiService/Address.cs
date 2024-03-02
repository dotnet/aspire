// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Search.Documents.Indexes;

namespace AzureSearch.ApiService;

public class Address
{
    [SearchableField(IsFilterable = true)]
    public string? StreetAddress { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string? City { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string? StateProvince { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string? PostalCode { get; set; }

    [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string? Country { get; set; }
}
