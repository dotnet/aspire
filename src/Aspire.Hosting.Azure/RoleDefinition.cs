// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a role definition within an Azure resource.
/// </summary>
/// <param name="Id">The unique identifier for the role definition.</param>
/// <param name="Name">The name of the role definition.</param>
public record struct RoleDefinition(string Id, string Name);
