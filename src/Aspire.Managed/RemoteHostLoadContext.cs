// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;

namespace Aspire.Managed;

/// <summary>
/// Loads the remote host payload and integration assemblies in an isolated assembly load context.
/// </summary>
internal sealed class RemoteHostLoadContext : AssemblyLoadContext
{
    private const string LoadContextName = "Aspire.Managed.RemoteHost";
    private const string ResourcePrefix = "Aspire.Managed.RemoteHostPayload.";

    private readonly string? _integrationLibsPath;
    private readonly Assembly _resourceAssembly;
    private readonly IReadOnlyDictionary<string, string> _resourceNames;
    private readonly IReadOnlySet<string> _sharedAssemblyNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteHostLoadContext"/> class.
    /// </summary>
    /// <param name="resourceAssembly">The assembly that contains embedded remote host payload resources.</param>
    /// <param name="sharedAssemblyNames">The assembly names that should always resolve from the default load context.</param>
    /// <param name="integrationLibsPath">The optional directory that contains integration assemblies.</param>
    internal RemoteHostLoadContext(
        Assembly resourceAssembly,
        IEnumerable<string> sharedAssemblyNames,
        string? integrationLibsPath = null)
        : base(LoadContextName, isCollectible: true)
    {
        ArgumentNullException.ThrowIfNull(resourceAssembly);
        ArgumentNullException.ThrowIfNull(sharedAssemblyNames);

        _integrationLibsPath = string.IsNullOrWhiteSpace(integrationLibsPath) ? null : integrationLibsPath;
        _resourceAssembly = resourceAssembly;
        _resourceNames = CreateResourceNames(resourceAssembly);
        _sharedAssemblyNames = CreateSharedAssemblyNames(sharedAssemblyNames);
    }

    /// <summary>
    /// Gets a value indicating whether the specified assembly should be shared with the default load context.
    /// </summary>
    /// <param name="assemblyName">The simple assembly name.</param>
    /// <returns><see langword="true"/> when the assembly should be shared; otherwise, <see langword="false"/>.</returns>
    internal bool ShouldShareAssembly(string assemblyName)
    {
        return !string.IsNullOrWhiteSpace(assemblyName) && _sharedAssemblyNames.Contains(assemblyName);
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is null)
        {
            return null;
        }

        if (ShouldShareAssembly(assemblyName.Name))
        {
            return LoadFromDefaultContext(assemblyName);
        }

        if (LoadEmbeddedAssembly(assemblyName.Name) is { } embeddedAssembly)
        {
            return embeddedAssembly;
        }

        return LoadIntegrationAssembly(assemblyName.Name);
    }

    /// <inheritdoc />
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_integrationLibsPath is not null)
        {
            var libraryPath = Path.Combine(_integrationLibsPath, $"{unmanagedDllName}.dll");
            if (File.Exists(libraryPath))
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
        }

        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    private static IReadOnlySet<string> CreateSharedAssemblyNames(IEnumerable<string> sharedAssemblyNames)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assemblyName in sharedAssemblyNames)
        {
            if (!string.IsNullOrWhiteSpace(assemblyName))
            {
                names.Add(assemblyName);
            }
        }

        return names;
    }

    private static IReadOnlyDictionary<string, string> CreateResourceNames(Assembly resourceAssembly)
    {
        var resourceNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in resourceAssembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = resourceName[ResourcePrefix.Length..];
            var assemblyName = Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(assemblyName))
            {
                resourceNames[assemblyName] = resourceName;
            }
        }

        return resourceNames;
    }

    private static Assembly LoadFromDefaultContext(AssemblyName assemblyName)
    {
        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly =>
                AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.Default &&
                string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        return existing ?? AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
    }

    private Assembly? LoadEmbeddedAssembly(string assemblyName)
    {
        if (!_resourceNames.TryGetValue(assemblyName, out var resourceName))
        {
            return null;
        }

        using var stream = _resourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Could not open embedded remote host payload resource '{resourceName}'.", resourceName);

        return LoadFromStream(stream);
    }

    private Assembly? LoadIntegrationAssembly(string assemblyName)
    {
        if (_integrationLibsPath is null)
        {
            return null;
        }

        var assemblyPath = Path.Combine(_integrationLibsPath, $"{assemblyName}.dll");
        return File.Exists(assemblyPath) ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}
