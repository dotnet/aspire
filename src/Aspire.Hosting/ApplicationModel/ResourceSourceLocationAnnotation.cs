// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Associates a resource with the source invocation that created it.
/// </summary>
/// <remarks>
/// Development tools use this annotation to distinguish declarations with the same resource name.
/// The source document hash allows tools to reject locations from an older version of the document.
/// </remarks>
public sealed class ResourceSourceLocationAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSourceLocationAnnotation"/> class.
    /// </summary>
    /// <param name="filePath">The absolute path to the source document.</param>
    /// <param name="lineNumber">The one-based source line number.</param>
    /// <param name="columnNumber">The one-based UTF-16 source column, or <c>null</c> when unavailable.</param>
    /// <param name="documentHash">The non-cryptographic fingerprint of the source document.</param>
    /// <exception cref="ArgumentException">The path or document hash is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A supplied line or column is not positive.</exception>
    public ResourceSourceLocationAnnotation(string filePath, int lineNumber, int? columnNumber, string documentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentHash);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        if (columnNumber is { } column)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(column, nameof(columnNumber));
        }

        FilePath = filePath;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        DocumentHash = documentHash;
    }

    /// <summary>
    /// Gets the absolute path to the document containing the invocation.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the one-based source line number.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the one-based UTF-16 source column, or <c>null</c> when unavailable.
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    /// Gets the non-cryptographic fingerprint of the source document.
    /// </summary>
    public string DocumentHash { get; }

    /// <summary>
    /// Gets the source method name, when available, to distinguish calls within a fluent expression.
    /// </summary>
    public string? MethodName { get; init; }
}
