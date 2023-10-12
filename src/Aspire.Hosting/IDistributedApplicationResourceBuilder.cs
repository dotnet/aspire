// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public interface IDistributedApplicationResourceBuilder<out T> where T : IDistributedApplicationResource
{
    IDistributedApplicationBuilder ApplicationBuilder { get; }
    T Resource { get; }
    IDistributedApplicationResourceBuilder<T> WithAnnotation<TAnnotation>() where TAnnotation : IDistributedApplicationResourceAnnotation, new() => WithAnnotation<TAnnotation>(new TAnnotation());
    IDistributedApplicationResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : IDistributedApplicationResourceAnnotation;
}
