// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Specifies the operator roles that should be assigned to operator principals for an Azure resource.
/// </summary>
/// <param name="roles">The set of roles that should be assigned to operator principals.</param>
/// <remarks>
/// This annotation is applied to Azure resources to define which roles should be assigned to operator principals
/// designated on compute environments. When an operator is specified on a compute environment (e.g., via WithOperator),
/// these roles will be automatically assigned to that operator for this resource.
/// </remarks>
public class OperatorRoleCallbackAnnotation(IReadOnlySet<RoleDefinition> roles) : IResourceAnnotation
{
    /// <summary>
    /// Gets the set of roles that should be assigned to operator principals.
    /// </summary>
    public IReadOnlySet<RoleDefinition> Roles { get; } = roles;
}
