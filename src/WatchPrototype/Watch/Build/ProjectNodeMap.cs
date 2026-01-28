// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Graph;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal readonly struct ProjectNodeMap(ProjectGraph graph, ILogger logger)
    {
        public readonly ProjectGraph Graph = graph;

        // full path of proj file to list of nodes representing all target frameworks of the project:
        public readonly IReadOnlyDictionary<string, IReadOnlyList<ProjectGraphNode>> Map = 
            graph.ProjectNodes.GroupBy(n => n.ProjectInstance.FullPath).ToDictionary(
                keySelector: static g => g.Key,
                elementSelector: static g => (IReadOnlyList<ProjectGraphNode>)[.. g]);

        public IReadOnlyList<ProjectGraphNode> GetProjectNodes(string projectPath)
        {
            if (Map.TryGetValue(projectPath, out var rootProjectNodes))
            {
                return rootProjectNodes;
            }

            logger.LogError("Project '{ProjectPath}' not found in the project graph.", projectPath);
            return [];
        }

        public ProjectGraphNode? TryGetProjectNode(string projectPath, string? targetFramework)
        {
            var projectNodes = GetProjectNodes(projectPath);
            if (projectNodes is [])
            {
                return null;
            }

            if (targetFramework == null)
            {
                if (projectNodes.Count > 1)
                {
                    logger.LogError("Project '{ProjectPath}' targets multiple frameworks. Specify which framework to run using '--framework'.", projectPath);
                    return null;
                }

                return projectNodes[0];
            }

            ProjectGraphNode? candidate = null;
            foreach (var node in projectNodes)
            {
                if (node.ProjectInstance.GetPropertyValue("TargetFramework") == targetFramework)
                {
                    if (candidate != null)
                    {
                        // shouldn't be possible:
                        logger.LogWarning("Project '{ProjectPath}' has multiple instances targeting {TargetFramework}.", projectPath, targetFramework);
                        return candidate;
                    }

                    candidate = node;
                }
            }

            if (candidate == null)
            {
                logger.LogError("Project '{ProjectPath}' doesn't have a target for {TargetFramework}.", projectPath, targetFramework);
            }

            return candidate;
        }
    }
}
