// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Utils;

internal static class ImageNameGenerator
{
    private static readonly SHA1 s_sha1 = SHA1.Create();

    public static string GenerateImageName<T>(this IResourceBuilder<T> builder) where T: IResource
    {
        var bytes = Encoding.UTF8.GetBytes(builder.ApplicationBuilder.AppHostDirectory);
        var hash = s_sha1.ComputeHash(bytes);
        var hex = Convert.ToHexString(hash).ToLower();
        return $"{builder.Resource.Name}-image-{hex}";
    }
}
