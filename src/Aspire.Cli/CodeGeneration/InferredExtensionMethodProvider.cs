// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.CodeGeneration.Models;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Provides extension methods by inferring them from package naming conventions.
/// Used when actual assemblies are not available (e.g., NuGet packages).
/// </summary>
internal sealed class InferredExtensionMethodProvider : IExtensionMethodProvider
{
    /// <inheritdoc />
    public List<ExtensionMethodModel> GetExtensionMethods(string packageId)
    {
        // Extract the resource name from the package ID
        // e.g., "Aspire.Hosting.Redis" -> "Redis"
        var parts = packageId.Split('.');
        var resourceName = parts.Length > 2 ? parts[^1] : parts[^1];

        var methods = new List<ExtensionMethodModel>();

        // Skip base packages that don't have Add* methods
        if (string.Equals(packageId, "Aspire.Hosting", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(packageId, "Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase))
        {
            return methods;
        }

        // Add the main "AddXxx" method
        methods.Add(new ExtensionMethodModel
        {
            Name = string.Create(CultureInfo.InvariantCulture, $"Add{resourceName}"),
            ExtendedType = "IDistributedApplicationBuilder",
            ReturnType = string.Create(CultureInfo.InvariantCulture, $"IResourceBuilder<{resourceName}Resource>"),
            ResourceType = string.Create(CultureInfo.InvariantCulture, $"{resourceName}Resource"),
            ContainingType = string.Create(CultureInfo.InvariantCulture, $"{resourceName}BuilderExtensions"),
            Parameters =
            [
                new ParameterModel
                {
                    Name = "builder",
                    Type = "IDistributedApplicationBuilder",
                    IsThis = true
                },
                new ParameterModel
                {
                    Name = "name",
                    Type = "string"
                }
            ]
        });

        return methods;
    }
}
