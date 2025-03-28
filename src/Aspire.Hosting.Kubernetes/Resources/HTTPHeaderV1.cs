// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an HTTP header with a name and value.
/// </summary>
/// <remarks>
/// This class is used to define HTTP headers, where the 'Name' property specifies the
/// header name, and the 'Value' property specifies the corresponding header value.
/// It is commonly utilized in context with HTTP requests, such as those in Kubernetes
/// probe definitions or other HTTP-based operations.
/// </remarks>
[YamlSerializable]
public sealed class HttpHeaderV1
{
    /// <summary>
    /// Gets or sets the name of the HTTP header.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Represents the value associated with the HTTP header.
    /// This property holds the string value assigned to the header.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string Value { get; set; } = null!;
}
