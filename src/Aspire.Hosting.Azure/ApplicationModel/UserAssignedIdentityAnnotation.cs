// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.ApplicationModel;

/// <summary>
/// Represents a user assigned identity that should be assigned to a project.
/// </summary>
public class UserAssignedIdentityAnnotation : CustomManifestOutputAnnotation
{
    /// <summary>
    /// The Environment Variable Prefix.
    /// </summary>
    public string EnvironmentVariablePrefix => ((UserAssignedIdentityDescriptor)Value).EnvironmentVariablePrefix;

    /// <summary>
    /// Initializes a new instance of <see cref="UserAssignedIdentityAnnotation"/>.
    /// </summary>
    /// <param name="envPrefix">Environment Variable prefix for the Client ID.</param>
    /// <param name="clientId">The identity's Client ID for usage within the app.</param>
    /// <param name="identityResourceId">The identity Resource ID for assignment to the container app.</param>
    public UserAssignedIdentityAnnotation(string envPrefix, string clientId, string identityResourceId) : base("userAssignedIdentities", new UserAssignedIdentityDescriptor(envPrefix, clientId, identityResourceId))
    {
    }
}

/// <summary>
/// User Assigned Identity Descriptor for output in to the manifest.
/// </summary>
public sealed record UserAssignedIdentityDescriptor
{
    /// <summary>
    /// Identity Client ID.
    /// </summary>
    [JsonPropertyName("clientId")]
    public string ClientId { get; init; }
    /// <summary>
    /// Identity Resource ID.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string IdentityResourceId { get; init; }
    /// <summary>
    /// Environment Variable Prefix.
    /// </summary>
    [JsonPropertyName("env")]
    public string EnvironmentVariablePrefix { get; init; }

    /// <summary>
    /// Creates a new <see cref="UserAssignedIdentityDescriptor"/>.
    /// </summary>
    /// <param name="envPrefix">Environment Variable prefix for the Client ID.</param>
    /// <param name="clientId">The identity's Client ID for usage within the app.</param>
    /// <param name="identityResourceId">The identity Resource ID for assignment to the container app.</param>
    public UserAssignedIdentityDescriptor(string envPrefix, string clientId, string identityResourceId) => (ClientId, IdentityResourceId, EnvironmentVariablePrefix) = (clientId, identityResourceId, envPrefix);
}
