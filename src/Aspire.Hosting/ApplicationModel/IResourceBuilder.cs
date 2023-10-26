// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public interface IResourceBuilder<out T> where T : IResource
{
    IDistributedApplicationBuilder ApplicationBuilder { get; }
    T Resource { get; }
    IResourceBuilder<T> WithAnnotation<TAnnotation>() where TAnnotation : IResourceAnnotation, new() => WithAnnotation(new TAnnotation());
    IResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : IResourceAnnotation;
}
