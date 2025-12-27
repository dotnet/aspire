// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Azure.AppService;

/// <summary>
/// Handles post-processing of generated bicep to inject @onlyIfNotExists() decorator.
/// </summary>
/// <remarks>
/// This is a temporary solution to add @onlyIfNotExists() decorator support to bicep resources.
/// Once Azure.Provisioning SDK natively supports this, this class can be removed.
/// </remarks>
internal static partial class BicepDecoratorWriter
{
    /// <summary>
    /// Post-processes generated bicep to add @onlyIfNotExists() decorators for resources.
    /// </summary>
    /// <param name="onlyIfNotExistsResources">The list of resource identifiers that should have the decorator.</param>
    /// <param name="generatedBicep">The originally generated bicep template.</param>
    /// <returns>The bicep template with decorators injected where needed.</returns>
    public static string InjectOnlyIfNotExistsDecorators(IEnumerable<string> onlyIfNotExistsResources, string generatedBicep)
    {
        var resourceList = onlyIfNotExistsResources.ToList();

        if (resourceList.Count == 0)
        {
            return generatedBicep;
        }

        var result = generatedBicep;

        foreach (var resourceIdentifier in resourceList)
        {
            // Find the resource declaration and inject the decorator
            // Pattern: resource <identifier> '<type>@<version>' = {
            // We want to change it to: @onlyIfNotExists()\nresource <identifier> '<type>@<version>' = {
            
            var pattern = $@"(resource\s+{Regex.Escape(resourceIdentifier)}\s+')";
            var replacement = "@onlyIfNotExists()\n$1";
            
            result = Regex.Replace(result, pattern, replacement, RegexOptions.Multiline);
        }

        return result;
    }
}
