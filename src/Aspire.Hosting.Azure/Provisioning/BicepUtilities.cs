// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Hashing;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure.Provisioning;

/// <summary>
/// Utility methods for working with Bicep resources.
/// </summary>
internal static class BicepUtilities
{
    // Known values since they will be filled in by the provisioner
    private static readonly string[] s_knownParameterNames =
    [
        AzureBicepResource.KnownParameters.PrincipalName,
        AzureBicepResource.KnownParameters.PrincipalId,
        AzureBicepResource.KnownParameters.PrincipalType,
        AzureBicepResource.KnownParameters.Location,
    ];

    /// <summary>
    /// Converts the parameters to a JSON object compatible with the ARM template.
    /// </summary>
    public static async Task SetParametersAsync(JsonObject parameters, AzureBicepResource resource, bool skipDynamicValues = false, CancellationToken cancellationToken = default)
    {
        // Convert the parameters to a JSON object
        foreach (var parameter in resource.Parameters)
        {
            if (skipDynamicValues &&
                (s_knownParameterNames.Contains(parameter.Key) || IsParameterWithGeneratedValue(parameter.Value)))
            {
                continue;
            }

            // Execute parameter values which are deferred.
            var parameterValue = parameter.Value is Func<object?> f ? f() : parameter.Value;

            parameters[parameter.Key] = new JsonObject()
            {
                ["value"] = parameterValue switch
                {
                    string s => s,
                    IEnumerable<string> s => new JsonArray(s.Select(s => JsonValue.Create(s)).ToArray()),
                    int i => i,
                    bool b => b,
                    Guid g => g.ToString(),
                    JsonNode node => node,
                    IValueProvider v => await v.GetValueAsync(cancellationToken).ConfigureAwait(false),
                    null => null,
                    _ => throw new NotSupportedException($"The parameter value type {parameterValue.GetType()} is not supported.")
                }
            };
        }
    }

    /// <summary>
    /// Sets the scope information for a Bicep resource.
    /// </summary>
    public static async Task SetScopeAsync(JsonObject scope, AzureBicepResource resource, CancellationToken cancellationToken = default)
    {
        // Resolve the scope from the AzureBicepResource if it has already been set
        // via the ConfigureInfrastructure callback. If not, fallback to the ExistingAzureResourceAnnotation.
        var targetScope = GetExistingResourceGroup(resource);

        scope["resourceGroup"] = targetScope switch
        {
            string s => s,
            IValueProvider v => await v.GetValueAsync(cancellationToken).ConfigureAwait(false),
            null => null,
            _ => throw new NotSupportedException($"The scope value type {targetScope.GetType()} is not supported.")
        };
    }

    /// <summary>
    /// Gets the checksum for a Bicep resource configuration.
    /// </summary>
    public static string GetChecksum(AzureBicepResource resource, JsonObject parameters, JsonObject? scope)
    {
        // TODO: PERF Inefficient

        // Combine the parameter values with the bicep template to create a unique value
        var input = parameters.ToJsonString() + resource.GetBicepTemplateString();
        if (scope is not null)
        {
            input += scope.ToJsonString();
        }

        // Hash the contents
        var hashedContents = Crc32.Hash(Encoding.UTF8.GetBytes(input));

        // Convert the hash to a string
        return Convert.ToHexString(hashedContents).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the current checksum for a Bicep resource from configuration.
    /// </summary>
    public static async ValueTask<string?> GetCurrentChecksumAsync(AzureBicepResource resource, IConfiguration section, CancellationToken cancellationToken = default)
    {
        // Fill in parameters from configuration
        if (section["Parameters"] is not string jsonString)
        {
            return null;
        }

        try
        {
            var parameters = JsonNode.Parse(jsonString)?.AsObject();
            var scope = section["Scope"] is string scopeString
                ? JsonNode.Parse(scopeString)?.AsObject()
                : null;

            if (parameters is null)
            {
                return null;
            }

            // Now overwrite with live object values skipping known and generated values.
            // This is important because the provisioner will fill in the known values and
            // generated values would change every time, so they can't be part of the checksum.
            await SetParametersAsync(parameters, resource, skipDynamicValues: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (scope is not null)
            {
                await SetScopeAsync(scope, resource, cancellationToken).ConfigureAwait(false);
            }

            // Get the checksum of the new values
            return GetChecksum(resource, parameters, scope);
        }
        catch
        {
            // Unable to parse the JSON, to treat it as not existing
            return null;
        }
    }

    private static object? GetExistingResourceGroup(AzureBicepResource resource) =>
        resource.Scope?.ResourceGroup ??
            (resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingResource) ?
                existingResource.ResourceGroup :
                null);

    private static bool IsParameterWithGeneratedValue(object? value)
    {
        return value is ParameterResource { Default: not null };
    }
}