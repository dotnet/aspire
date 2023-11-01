// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace RatingsService;

public class Rating
{
    /// <summary>A unique id for this rating.</summary>
    [Required, JsonProperty("id")]
    public string RatingId { get; set; } = "";

    /// <summary>The ProductId that this rating is for.</summary>
    [Required]
    public string ProductId { get; set; } = "";

    /// <summary>The value of this rating (from 1 to 5).</summary>
    [Range(1, 5)]
    public int RatingValue { get; set; }

    /// <summary>The user who gave this rating (optional).</summary>
    public string? User { get; set; }

    /// <summary>The text of the review (optional).</summary>
    public string? Review { get; set; }
}
