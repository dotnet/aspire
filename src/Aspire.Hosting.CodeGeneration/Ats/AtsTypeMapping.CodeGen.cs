// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Ats;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.Ats;

public sealed partial class AtsTypeMapping
{
    /// <summary>
    /// Creates a type mapping by scanning assemblies using metadata reflection.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A type mapping containing all discovered type mappings.</returns>
    public static AtsTypeMapping FromRoAssemblies(IEnumerable<RoAssembly> assemblies)
    {
        return FromAssemblies(assemblies.Select(a => new RoAssemblyInfoWrapper(a)));
    }
}
