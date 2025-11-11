// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Represents a file pattern for copying dependency files in a Dockerfile.
/// </summary>
/// <param name="Source">The source pattern for files to copy (e.g., "package*.json").</param>
/// <param name="Destination">The destination path where files should be copied (e.g., "./").</param>
public sealed record CopyFilePattern(string Source, string Destination);
