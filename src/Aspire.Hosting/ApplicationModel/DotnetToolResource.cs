// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a .NET tool resource that encapsulates metadata about a .NET CLI tool, including its name, package ID,
/// and command.
/// </summary>
/// <remarks>This class is used to define and manage resources for .NET CLI tools. It associates a tool's name and
/// command with its package ID, and ensures that the required metadata is properly annotated.</remarks>
[Experimental("ASPIREDOTNETTOOL", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class DotnetToolResource : ExecutableResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetToolResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="packageId">The package id of the tool.</param>
    public DotnetToolResource(string name, string packageId)
        : base(name, "dotnet", string.Empty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId, nameof(packageId));
        Annotations.Add(new DotnetToolAnnotation { PackageId = packageId });
    }

    internal DotnetToolAnnotation? ToolConfiguration
    {
        get
        {
            this.TryGetLastAnnotation<DotnetToolAnnotation>(out var toolConfig);
            return toolConfig;
        }
    }
}
