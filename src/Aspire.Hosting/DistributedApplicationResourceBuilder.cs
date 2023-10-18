// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationResourceBuilder<T>(IDistributedApplicationBuilder applicationBuilder, T resource) : IDistributedApplicationResourceBuilder<T> where T : IDistributedApplicationResource
{
    public T Resource { get; } = resource;
    public IDistributedApplicationBuilder ApplicationBuilder { get; } = applicationBuilder;

    public IDistributedApplicationResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : IDistributedApplicationResourceAnnotation
    {
        Resource.Annotations.Add(annotation);
        return this;
    }
}
