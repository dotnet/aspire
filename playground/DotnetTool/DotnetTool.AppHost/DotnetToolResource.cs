#pragma warning disable IDE0005 // Using directive is unnecessary (needed when file is linked to test project)
using Aspire.Hosting.ApplicationModel;
#pragma warning restore IDE0005

namespace DotnetTool.AppHost;

/// <summary>
/// Represents a .NET tool resource that encapsulates metadata about a .NET CLI tool, including its name, package ID,
/// and command.
/// </summary>
/// <remarks>This class is used to define and manage resources for .NET CLI tools. It associates a tool's name and
/// command with its package ID, and ensures that the required metadata is properly annotated.</remarks>
public class DotnetToolResource : ExecutableResource
{
    /// <param name="name">The name of the resource.</param>
    /// <param name="packageId">The package id of the tool</param>
    public DotnetToolResource(string name, string packageId) 
        : base(name, "dotnet", ".")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId, nameof(packageId));
        Annotations.Add(new DotNetToolAnnotation { PackageId = packageId });
    }

    internal DotNetToolAnnotation ToolConfiguration
    {
        get
        {
            if (!this.TryGetLastAnnotation<DotNetToolAnnotation>(out var toolConfig))
            {
                throw new InvalidOperationException("DotNetToolAnnotation is missing");
            }
            return toolConfig;
        }
    }
}
