// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="InputAnnotation"/>.
/// </summary>
internal static class InputAnnotationExtensions
{
    internal static IResourceBuilder<T> WithDefaultPassword<T>(this IResourceBuilder<T> builder) where T : IResource
    {
        builder.WithAnnotation(new InputAnnotation("password", secret: true)
        {
            Default = new GenerateInputDefault { MinLength = 10 }
        });

        return builder;
    }
}
