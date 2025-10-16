// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Defines the format of a <see cref="ReferenceExpression"/>.
/// </summary>
public enum ReferenceExpressionFormats
{
    /// <summary>
    /// No special formatting.
    /// </summary>
    None = 0,

    /// <summary>
    /// The value should be URL-encoded.
    /// </summary>
    UrlEncoded = 1,
}

internal class ManifestEncoder : IReferenceExpressionEncoder
{
    public string EncodeValue(string value, ReferenceExpressionFormats format)
    {
        return format switch
        {
            ReferenceExpressionFormats.None => value,
            ReferenceExpressionFormats.UrlEncoded => $"uriComponent({value})",
            _ => throw new NotSupportedException($"The format '{format}' is not supported."),
        };
    }
}

internal class ValueEncoder : IReferenceExpressionEncoder
{
    public string EncodeValue(string value, ReferenceExpressionFormats format)
    {
        return format switch
        {
            ReferenceExpressionFormats.None => value,
            ReferenceExpressionFormats.UrlEncoded => Uri.EscapeDataString(value),
            _ => throw new NotSupportedException($"The format '{format}' is not supported."),
        };
    }
}

/// <summary>
/// Provides methods to encode values and Bicep expressions based on the specified <see cref="ReferenceExpressionFormats"/>.
/// </summary>
public interface IReferenceExpressionEncoder
{
    /// <summary>
    /// Encodes the given value according to the specified format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    string EncodeValue(string value, ReferenceExpressionFormats format);
}