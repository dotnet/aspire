// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;

namespace Microsoft.DotNet.Watch;

internal static class ProjectGraphUtilities
{
    public static string GetDisplayName(this ProjectGraphNode projectNode)
        => projectNode.ProjectInstance.GetDisplayName();

    public static string GetDisplayName(this ProjectInstance project)
        => $"{Path.GetFileNameWithoutExtension(project.FullPath)} ({project.GetTargetFramework()})";

    public static string GetTargetFramework(this ProjectInstance project)
        => project.GetPropertyValue(PropertyNames.TargetFramework);

    public static IEnumerable<string> GetTargetFrameworks(this ProjectInstance project)
        => project.GetStringListPropertyValue(PropertyNames.TargetFrameworks);

    public static Version? GetTargetFrameworkVersion(this ProjectGraphNode projectNode)
    {
        try
        {
            return new FrameworkName(projectNode.ProjectInstance.GetPropertyValue(PropertyNames.TargetFrameworkMoniker)).Version;
        }
        catch
        {
            return null;
        }
    }

    public static IEnumerable<string> GetWebAssemblyCapabilities(this ProjectGraphNode projectNode)
        => projectNode.GetStringListPropertyValue(PropertyNames.WebAssemblyHotReloadCapabilities);

    public static bool IsTargetFrameworkVersionOrNewer(this ProjectGraphNode projectNode, Version minVersion)
        => projectNode.GetTargetFrameworkVersion() is { } version && version >= minVersion;

    public static bool IsNetCoreApp(string identifier)
        => string.Equals(identifier, ".NETCoreApp", StringComparison.OrdinalIgnoreCase);

    public static bool IsNetCoreApp(this ProjectGraphNode projectNode)
        => IsNetCoreApp(projectNode.ProjectInstance.GetPropertyValue(PropertyNames.TargetFrameworkIdentifier));

    public static bool IsNetCoreApp(this ProjectGraphNode projectNode, Version minVersion)
        => projectNode.IsNetCoreApp() && projectNode.IsTargetFrameworkVersionOrNewer(minVersion);

    public static bool IsWebApp(this ProjectGraphNode projectNode)
        => projectNode.GetCapabilities().Any(static value => value is ProjectCapability.AspNetCore or ProjectCapability.WebAssembly);

    public static string? GetOutputDirectory(this ProjectGraphNode projectNode)
        => projectNode.ProjectInstance.GetPropertyValue(PropertyNames.TargetPath) is { Length: >0 } path ? Path.GetDirectoryName(Path.Combine(projectNode.ProjectInstance.Directory, path)) : null;

    public static string GetAssemblyName(this ProjectGraphNode projectNode)
        => projectNode.ProjectInstance.GetPropertyValue(PropertyNames.TargetName);

    public static string? GetIntermediateOutputDirectory(this ProjectInstance project)
        => project.GetPropertyValue(PropertyNames.IntermediateOutputPath) is { Length: >0 } path ? Path.Combine(project.Directory, path) : null;

    public static IEnumerable<string> GetCapabilities(this ProjectGraphNode projectNode)
        => projectNode.ProjectInstance.GetItems(ItemNames.ProjectCapability).Select(item => item.EvaluatedInclude);

    public static bool IsAutoRestartEnabled(this ProjectGraphNode projectNode)
        => projectNode.GetBooleanPropertyValue(PropertyNames.HotReloadAutoRestart);

    public static bool AreDefaultItemsEnabled(this ProjectGraphNode projectNode)
        => projectNode.GetBooleanPropertyValue(PropertyNames.EnableDefaultItems);

    public static IEnumerable<string> GetDefaultItemExcludes(this ProjectGraphNode projectNode)
        => projectNode.GetStringListPropertyValue(PropertyNames.DefaultItemExcludes);

    public static IEnumerable<string> GetStringListPropertyValue(this ProjectGraphNode projectNode, string propertyName)
        => projectNode.ProjectInstance.GetStringListPropertyValue(propertyName);

    public static IEnumerable<string> GetStringListPropertyValue(this ProjectInstance project, string propertyName)
        => project.GetPropertyValue(propertyName).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public static bool GetBooleanPropertyValue(this ProjectGraphNode projectNode, string propertyName, bool defaultValue = false)
        => GetBooleanPropertyValue(projectNode.ProjectInstance, propertyName, defaultValue);

    public static bool GetBooleanPropertyValue(this ProjectInstance project, string propertyName, bool defaultValue = false)
        => project.GetPropertyValue(propertyName) is { Length: >0 } value ? bool.TryParse(value, out var result) && result : defaultValue;

    public static bool GetBooleanMetadataValue(this ProjectItemInstance item, string metadataName, bool defaultValue = false)
        => item.GetMetadataValue(metadataName) is { Length: > 0 } value ? bool.TryParse(value, out var result) && result : defaultValue;

    public static IEnumerable<ProjectGraphNode> GetAncestorsAndSelf(this ProjectGraphNode project)
        => GetAncestorsAndSelf([project]);

    public static IEnumerable<ProjectGraphNode> GetAncestorsAndSelf(this IEnumerable<ProjectGraphNode> projects)
        => GetTransitiveProjects(projects, static project => project.ReferencingProjects);

    public static IEnumerable<ProjectGraphNode> GetDescendantsAndSelf(this ProjectGraphNode project)
        => GetDescendantsAndSelf([project]);

    public static IEnumerable<ProjectGraphNode> GetDescendantsAndSelf(this IEnumerable<ProjectGraphNode> projects)
        => GetTransitiveProjects(projects, static project => project.ProjectReferences);

    private static IEnumerable<ProjectGraphNode> GetTransitiveProjects(IEnumerable<ProjectGraphNode> projects, Func<ProjectGraphNode, IEnumerable<ProjectGraphNode>> getEdges)
    {
        var visited = new HashSet<ProjectGraphNode>();
        var queue = new Queue<ProjectGraphNode>();
        foreach (var project in projects)
        {
            queue.Enqueue(project);
        }

        while (queue.Count > 0)
        {
            var project = queue.Dequeue();
            if (visited.Add(project))
            {
                yield return project;

                foreach (var referencingProject in getEdges(project))
                {
                    queue.Enqueue(referencingProject);
                }
            }
        }
    }

    public static ProjectInstanceId GetId(this ProjectInstance project)
        => new(project.FullPath, project.GetTargetFramework());
}
