// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Aspire.Dashboard.Extensions;

internal static class ClaimsIdentityExtensions
{
    /// <summary>
    /// Searches the claims in the <see cref="ClaimsIdentity.Claims"/> for each of the claim types in <paramref name="claimTypes" />
    /// in the order presented and returns the first one that it finds.
    /// </summary>
    public static string? FindFirst(this ClaimsIdentity identity, string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var claim = identity.FindFirst(claimType);
            if (claim is not null)
            {
                return claim.Value;
            }
        }

        return null;
    }
}
