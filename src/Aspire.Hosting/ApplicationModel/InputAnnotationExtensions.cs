// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="InputAnnotation"/>.
/// </summary>
internal static class InputAnnotationExtensions
{
    internal static T WithDefaultGeneratedPasswordAnnotation<T>(this T builder)
        where T : IResourceBuilder<ContainerResource>
    {
        builder.WithAnnotation(new InputAnnotation("password", secret: true)
        {
            Default = new GenerateInputDefault { MinLength = 10 }
        });

        return builder;
    }
}
