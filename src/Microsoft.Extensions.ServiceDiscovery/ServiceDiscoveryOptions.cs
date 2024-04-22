// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Options for service endpoint resolution.
/// </summary>
public sealed class ServiceDiscoveryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether all URI schemes for URIs resolved by the service discovery system are allowed.
    /// If this value is <see langword="true"/>, all URI schemes are allowed.
    /// If this value is <see langword="false"/>, only the schemes specified in <see cref="AllowedSchemes"/> are allowed.
    /// </summary>
    public bool AllowAllSchemes { get; set; } = true;

    /// <summary>
    /// Gets or sets the period between polling attempts for providers which do not support refresh notifications via <see cref="IChangeToken.ActiveChangeCallbacks"/>.
    /// </summary>
    public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the collection of allowed URI schemes for URIs resolved by the service discovery system when multiple schemes are specified, for example "https+http://_endpoint.service".
    /// </summary>
    /// <remarks>
    /// When <see cref="AllowAllSchemes"/> is <see langword="true"/>, this property is ignored.
    /// </remarks>
    public IList<string> AllowedSchemes { get; set; } = new List<string>();

    internal static string[] ApplyAllowedSchemes(IReadOnlyList<string> schemes, IList<string> allowedSchemes, bool allowAllSchemes)
    {
        if (schemes.Count > 0)
        {
            if (allowAllSchemes)
            {
                if (schemes is string[] array && array.Length > 0)
                {
                    return array;
                }

                return schemes.ToArray();
            }

            List<string> result = [];
            foreach (var s in schemes)
            {
                foreach (var allowed in allowedSchemes)
                {
                    if (string.Equals(s, allowed, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(s);
                        break;
                    }
                }
            }

            return result.ToArray();
        }

        // If no schemes were specified, but a set of allowed schemes were specified, allow those.
        return allowedSchemes.ToArray();
    }
}
