// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="Description"></param>

// REVIEW: This should be part of the Azure.Provisioning APIs
public record struct RoleDefinition(string Id, string Description);
