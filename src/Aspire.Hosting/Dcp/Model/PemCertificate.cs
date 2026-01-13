// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Dcp.Model;

// Represents a public PEM encoded certificate
internal sealed class PemCertificate
{
    // Thumbprint of the certificate
    [JsonPropertyName("thumbprint")]
    public string? Thumbprint { get; set; }

    // The PEM encoded contents of the public certificate
    [JsonPropertyName("contents")]
    public string? Contents { get; set; }
}