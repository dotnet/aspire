// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for <see cref="ParameterResource"/>.
/// </summary>
public static class ParameterResourceExtensions
{
    /// <summary>
    /// Determines whether the resource is a parameter.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static bool IsParameter(this IResource resource) =>
        resource.ResourceKind.IsAssignableTo(typeof(ParameterResource));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="parameterResource"></param>
    /// <returns></returns>
    public static bool TryGetParameter(this IResource resource, [NotNullWhen(true)] out ParameterResource? parameterResource)
    {
        if (resource.IsParameter())
        {
            parameterResource = resource as ParameterResource ?? new ParameterResource(resource.Name, resource.Annotations);
            return true;
        }

        parameterResource = null;
        return false;
    }
}
