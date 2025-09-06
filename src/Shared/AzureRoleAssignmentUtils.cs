// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

internal static class AzureRoleAssignmentUtils
{
    internal static IResourceBuilder<T> WithRoleAssignments<T, TTarget, TBuiltInRole>(this IResourceBuilder<T> builder, IResourceBuilder<TTarget> target, Func<TBuiltInRole, string> getName, TBuiltInRole[] roles)
        where T : IResource
        where TTarget : AzureProvisioningResource
        where TBuiltInRole : notnull
    {
        return builder.WithAnnotation(new RoleAssignmentAnnotation(target.Resource, CreateRoleDefinitions(roles, getName)));
    }

    internal static IResourceBuilder<T> WithDefaultRoleAssignments<T, TBuiltInRole>(this IResourceBuilder<T> builder, Func<TBuiltInRole, string> getName, params TBuiltInRole[] roles)
        where T : IResource
        where TBuiltInRole : notnull
    {
        return builder.WithAnnotation(new DefaultRoleAssignmentsAnnotation(CreateRoleDefinitions(roles, getName)));
    }

    private static HashSet<RoleDefinition> CreateRoleDefinitions<TBuiltInRole>(IReadOnlyList<TBuiltInRole> roles, Func<TBuiltInRole, string> getName)
        where TBuiltInRole : notnull
    {
        return [.. roles.Select(r => new RoleDefinition(r.ToString()!, getName(r)))];
    }
}
