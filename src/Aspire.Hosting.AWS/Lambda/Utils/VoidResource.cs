// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Lambda.Utils;

/// <summary>
///
/// </summary>
/// <seealso cref="Resource"/>
internal sealed class VoidResource : Resource
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    private VoidResource(string name) : base(name)
    {
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="resources"></param>
    /// <returns></returns>
    public static VoidResource CreateWithEnvironmentVariables(params IResource[] resources)
    {
        var voidResource = new VoidResource("v" + Guid.NewGuid());

        foreach (var resource in resources)
        {
            foreach (var envAnnotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            {
                voidResource.Annotations.Add(envAnnotation);
            }
        }

        return voidResource;
    }
}
