// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationComponentBuilder<T>(IDistributedApplicationBuilder applicationBuilder, T component) : IDistributedApplicationComponentBuilder<T> where T : IDistributedApplicationComponent
{
    public T Component { get; } = component;
    public IDistributedApplicationBuilder ApplicationBuilder { get; } = applicationBuilder;

    public IDistributedApplicationComponentBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : IDistributedApplicationComponentAnnotation
    {
        Component.Annotations.Add(annotation);
        return this;
    }
}
