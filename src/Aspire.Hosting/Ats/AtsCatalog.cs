// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Catalog of ATS capabilities and types for a loaded set of assemblies.
/// </summary>
internal sealed class AtsCatalog
{
    private readonly AtsContext _context;

    private AtsCatalog(AtsContext context)
    {
        _context = context;
    }

    public static AtsCatalog Create(IReadOnlyList<Assembly> assemblies)
    {
        var scanResult = AtsCapabilityScanner.ScanAssemblies(assemblies);
        return new AtsCatalog(scanResult.ToAtsContext());
    }

    public AtsContext GetContext() => _context;

    public object GetIsolatedContext() => _context;

    public AtsSession CreateSession(Func<string, JsonNode?, CancellationToken, Task<JsonNode?>> callbackInvoker)
    {
        return new AtsSession(_context, callbackInvoker);
    }
}
