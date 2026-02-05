// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json.Serialization;

namespace Aspire.Tools.Service;

/// <summary>
/// Detailed error information serialized into the body of the response
/// </summary>
internal class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorDetail? Error { get; set; }
}

internal class ErrorDetail
{
    [JsonPropertyName("code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public ErrorDetail[]? MessageDetails { get; set; }
}
