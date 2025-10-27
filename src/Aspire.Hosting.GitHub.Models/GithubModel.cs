// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.GitHub;

/// <summary>
/// Represents a model published on GitHub.
/// </summary>
public partial class GitHubModel
{
    /// <summary>
    /// The unique identifier for the model.
    /// </summary>
    public required string Id { get; init; }
}