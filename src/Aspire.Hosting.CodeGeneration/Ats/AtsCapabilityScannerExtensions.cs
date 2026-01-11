// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration.Ats;

/// <summary>
/// Extension methods for AtsCapabilityScanner that provide a simpler API for code generation scenarios.
/// These methods wrap the shared scanner's IAtsAssemblyInfo-based API with RoAssembly-based overloads.
/// </summary>
internal static class AtsCapabilityScannerExtensions
{
    /// <summary>
    /// Scans an assembly for capabilities.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <returns>A list of capabilities found in the assembly.</returns>
    public static List<AtsCapabilityInfo> ScanAssembly(
        RoAssembly assembly,
        AtsTypeMapping typeMapping)
    {
        var wrapper = new RoAssemblyInfoWrapper(assembly);
        var result = AtsCapabilityScanner.ScanAssembly(wrapper, typeMapping);
        return result.Capabilities;
    }

    /// <summary>
    /// Scans multiple assemblies for capabilities using 2-pass scanning.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <returns>A list of capabilities found in all assemblies.</returns>
    public static List<AtsCapabilityInfo> ScanAssemblies(
        IEnumerable<RoAssembly> assemblies,
        AtsTypeMapping typeMapping)
    {
        var wrappers = assemblies.Select(a => new RoAssemblyInfoWrapper(a));
        var result = AtsCapabilityScanner.ScanAssemblies(wrappers, typeMapping);
        return result.Capabilities;
    }

    /// <summary>
    /// Scans multiple assemblies for capabilities and type info using 2-pass scanning.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <returns>The scan result containing capabilities and type infos.</returns>
    public static AtsCapabilityScanner.ScanResult ScanAssembliesWithTypeInfo(
        IEnumerable<RoAssembly> assemblies,
        AtsTypeMapping typeMapping)
    {
        var wrappers = assemblies.Select(a => new RoAssemblyInfoWrapper(a));
        return AtsCapabilityScanner.ScanAssemblies(wrappers, typeMapping);
    }

    /// <summary>
    /// Scans an assembly and returns an AtsContext for code generation.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <returns>An AtsContext containing the scan results.</returns>
    public static AtsContext ScanAssemblyToContext(
        RoAssembly assembly,
        AtsTypeMapping typeMapping)
    {
        var wrapper = new RoAssemblyInfoWrapper(assembly);
        var result = AtsCapabilityScanner.ScanAssembly(wrapper, typeMapping);
        return result.ToAtsContext();
    }

    /// <summary>
    /// Scans multiple assemblies and returns an AtsContext for code generation.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="typeMapping">The type mapping for resolving ATS type IDs.</param>
    /// <returns>An AtsContext containing the scan results.</returns>
    public static AtsContext ScanAssembliesToContext(
        IEnumerable<RoAssembly> assemblies,
        AtsTypeMapping typeMapping)
    {
        var wrappers = assemblies.Select(a => new RoAssemblyInfoWrapper(a));
        var result = AtsCapabilityScanner.ScanAssemblies(wrappers, typeMapping);
        return result.ToAtsContext();
    }
}
