// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace Aspire.Hosting.Tasks;

/// <summary>
/// Gets the references in 'AppProjectTargetFramework' that aren't executable projects.
/// </summary>
public sealed class GetNonExecutableReferences : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The project target frameworks to check.
    /// </summary>
    [Required]
    public ITaskItem[] AppProjectTargetFramework { get; set; } = [];

    /// <summary>
    /// The output containing non-executable references.
    /// </summary>
    [Output]
    public ITaskItem[] NonExecutableReferences { get; set; } = [];

    public override bool Execute()
    {
        var nonExecutableReferences = new System.Collections.Generic.List<ITaskItem>();

        foreach (var appProject in AppProjectTargetFramework)
        {
            var additionalProperties = appProject.GetMetadata("AdditionalPropertiesFromProject");
            if (string.IsNullOrEmpty(additionalProperties))
            {
                // Skip any projects that don't contain the right metadata
                continue;
            }

            var additionalPropertiesXml = XElement.Parse(additionalProperties);
            foreach (var targetFrameworkElement in additionalPropertiesXml.Elements())
            {
                var isExe = targetFrameworkElement.Element("_IsExecutable");
                if (isExe != null && !string.Equals(isExe.Value, "true", System.StringComparison.OrdinalIgnoreCase))
                {
                    nonExecutableReferences.Add(appProject);
                }
            }
        }

        NonExecutableReferences = nonExecutableReferences.ToArray();
        return true;
    }
}
