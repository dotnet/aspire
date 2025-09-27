// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Rosetta.Models;

namespace Rosetta;

/// <summary>
/// Represents the aspire.json file used by the tool.
/// It references all the Aspire Hosting packages used in the project.
/// </summary>
public class PackagesJson
{
    const string FolderPrefix = ".aspire";
    const string PackagesFileName = "aspire.json";
    const string NugetCacheFileName = "packages.json";
    const string NugetFeedUrl = "https://azuresearch-usnc.nuget.org/query?q=tag:aspire+integration+hosting&take=1000";

    private string _packageJsonPath;
    private static readonly HashSet<string> s_packageNames;

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static PackagesJson()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        var nugetCache = Path.Combine(Path.GetTempPath(), FolderPrefix, NugetCacheFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(nugetCache)!);

        if (!File.Exists(nugetCache) || new FileInfo(nugetCache).CreationTimeUtc < DateTime.UtcNow.AddDays(-1))
        {
            using var httpClient = new HttpClient();
            var result = httpClient.GetStringAsync(NugetFeedUrl).Result;
            File.WriteAllText(nugetCache, result);
        }

        var doc = JsonObject.Parse(File.ReadAllText(nugetCache));
        s_packageNames = doc!["data"]!.AsArray().Select(x => $"{x!.AsObject()["id"]}@{x.AsObject()["version"]}").ToHashSet();
    }

    public List<string> Names { get; set; } = [];

    /// <summary>
    /// Retrieves a collection of package names and their versions from the Names collection. Each package is split by
    /// the '@' character.
    /// </summary>
    /// <returns>Returns an enumerable of tuples containing the package name and version.</returns>
    public IEnumerable<(string Name, string Version)> GetPackages()
    {
        foreach (var package in Names)
        {
            if (package.Split('@', 2) is not [var name, var version])
            {
                continue;
            }

            yield return (name, version);
        }
    }

    /// <summary>
    /// Computes a SHA256 hash of the package names and versions.
    /// </summary>
    public string GetPackagesHash()
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join(";", Names.Select(n => n.ToLowerInvariant()).OrderBy(n => n))));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Required for JSON deserialization
    /// </summary>
    public PackagesJson()
    {
        _packageJsonPath = "";
    }

    /// <summary>
    /// Opens a packages.json file from a specified directory path and deserializes its content.
    /// </summary>
    /// <param name="appPath">Specifies the directory path where the PackagesJson file is located.</param>
    /// <returns>Returns a PackagesJson object containing the deserialized data from the file.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    public static PackagesJson Open(string appPath)
    {
        if (!Directory.Exists(appPath))
        {
            throw new DirectoryNotFoundException($"The directory '{appPath}' does not exist.");
        }

        var packageJsonPath = Path.Combine(appPath, PackagesFileName);

        if (!File.Exists(packageJsonPath))
        {
            var empty = new PackagesJson() { _packageJsonPath = packageJsonPath };
            empty.Import("Aspire.Hosting.AppHost", ProjectModel.AspireHostVersion);
        }

        using var packagesFile = new FileStream(packageJsonPath, FileMode.Open, FileAccess.Read);

        var result = JsonSerializer.Deserialize(packagesFile, typeof(PackagesJson), PackagesJsonSerializerContext.Default) as PackagesJson;

        if (result is null)
        {
            throw new InvalidOperationException($"Failed to deserialize the file '{packageJsonPath}'.");
        }

        result._packageJsonPath = packageJsonPath;

        return result;
    }

    /// <summary>
    /// Imports a package by its name and version, updating the internal list and saving to a JSON file.
    /// </summary>
    /// <param name="name">Specifies the name of the package to be imported.</param>
    /// <param name="version">Indicates the version of the package to be imported.</param>
    public void Import(string name, string version)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(version);

        var packageName = $"{name}@{version}";

        Names.RemoveAll(s => s.Equals(packageName, StringComparison.OrdinalIgnoreCase));

        Names.Add(packageName);
        File.WriteAllText(_packageJsonPath, JsonSerializer.Serialize(this, typeof(PackagesJson), PackagesJsonSerializerContext.Default));
    }

    /// <summary>
    /// Searches a NuGet package by a part of its name from the NuGet.org package feed.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static (string, string)? GetPackageByShortName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (name == "Aspire.Hosting.AppHost")
        {
            return ("Aspire.Hosting.AppHost", ProjectModel.AspireHostVersion);
        }

        var package = s_packageNames.FirstOrDefault(p => p.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? s_packageNames.FirstOrDefault(p => p.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(package))
        {
            return null;
        }

        if (package.Split('@', 2) is not [var id, var version])
        {
            return null;
        }

        return (id, version);
    }

    public MetadataLoadContext GetMetadataLoadContext(IDependencyContext dependencyContext)
    {
        var artifactsAssemblies = new List<string>(Directory.GetFiles(dependencyContext.ArtifactsPath, "*.dll"));
        var runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

        var resolver = new PathAssemblyResolver(artifactsAssemblies.Union(runtimeAssemblies));

        // Use MetadataLoadContext to isolate the types from Aspire.Hosting since this project will
        // be built using a different version than the one we load dynamically.
        var metadataLoadContext = new MetadataLoadContext(resolver);
        //var wellKnownTypes = new WellKnownTypes([metadataLoadContext.LoadFromAssemblyName("Aspire.Hosting"), metadataLoadContext.LoadFromAssemblyName("System.Private.CoreLib")]);

        //return (metadataLoadContext, wellKnownTypes);

        return metadataLoadContext;
    }

    /// <summary>
    /// Resolves integrations based on provided dependency context and returns a list of integration models, including all transitive packages.
    /// </summary>
    /// <param name="dependencyContext">Provides access to assembly paths and dependency information for resolving integrations.</param>
    /// <param name="debug"></param>
    /// <returns>A list of integration models that have been resolved.</returns>
    public List<IntegrationModel> ResolveIntegrations(IDependencyContext dependencyContext, bool debug)
    {
        var queue = new Queue<(string, string)>();
        var visited = new HashSet<string>();
        var integrationReverseLookup = new Dictionary<IntegrationModel, (string, string)>();
        var integrationLookup = new Dictionary<(string, string), List<IntegrationModel>>();

        var metadataLoadContext = GetMetadataLoadContext(dependencyContext);

        foreach (var (name, version) in GetPackages())
        {
            queue.Enqueue((name, version));
        }

        while (queue.TryDequeue(out var package))
        {
            var (name, version) = package;
            var key = $"{name}/{version}";

            if (!visited.Add(key))
            {
                continue;
            }

            if (debug)
            {
                Console.WriteLine($"Resolving {name}@{version}...");
            }

            var assemblies = new List<Assembly>();

            foreach (var assemblyFullPath in dependencyContext.GetAssemblyPaths(name, version))
            {
                assemblies.Add(metadataLoadContext.LoadFromAssemblyPath(assemblyFullPath));
            }

            var wellKnownTypes = new WellKnownTypes(assemblies.Union([
                metadataLoadContext.LoadFromAssemblyName("Aspire.Hosting"),
                metadataLoadContext.LoadFromAssemblyName("System.Runtime"),
                metadataLoadContext.LoadFromAssemblyName("System.Private.CoreLib")
                ]));

            foreach (var assembly in assemblies)
            {
                var integration = IntegrationModel.Create(wellKnownTypes, assembly);

                integrationReverseLookup[integration] = package;
                (CollectionsMarshal.GetValueRefOrAddDefault(integrationLookup, package, out _) ??= []).Add(integration);
            }

            foreach (var (depName, depVersion) in dependencyContext.GetDependencies(name, version))
            {
                var depBfsKey = $"{depName}/{depVersion}";
                var depsPackageKey = $"{depName}@{depVersion}";

                if (depName.Contains("Aspire.Hosting") || (s_packageNames.Contains(depsPackageKey) && !visited.Contains(depBfsKey)))
                {
                    queue.Enqueue((depName, depVersion));
                }
            }
        }

        foreach (var (integration, package) in integrationReverseLookup)
        {
            // Get the package name and version from the integration model
            var (name, version) = package;

            // Get all dependencies for the integration
            foreach (var dp in dependencyContext.GetDependencies(name, version))
            {
                if (integrationLookup.TryGetValue(dp, out var dependencies))
                {
                    integration.Dependencies.AddRange(dependencies);
                }
            }
        }

        return [.. integrationReverseLookup.Keys];
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, WriteIndented = true)]
[JsonSerializable(typeof(PackagesJson))]
internal partial class PackagesJsonSerializerContext : JsonSerializerContext
{

}

