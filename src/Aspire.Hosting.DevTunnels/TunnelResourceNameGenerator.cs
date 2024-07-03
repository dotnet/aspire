// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DevTunnels;

internal static class DevTunnelResourceNameGenerator
{
    private static readonly SHA1 s_sha1 = SHA1.Create();

    public static string GenerateTunnelName<T>(this IResourceBuilder<T> builder, string endpointName) where T : IResource
    {
        var bytes = Encoding.UTF8.GetBytes(builder.ApplicationBuilder.AppHostDirectory);
        var hash = s_sha1.ComputeHash(bytes);
        var hex = Convert.ToHexString(hash).ToLower();
        var tunnelName = $"{builder.Resource.Name}-{endpointName}-{hex}";
        return tunnelName.Length > 60 ? tunnelName.Substring(0, 60) : tunnelName;
    }
}
