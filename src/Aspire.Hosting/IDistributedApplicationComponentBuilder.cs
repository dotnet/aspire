// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public interface IDistributedApplicationComponentBuilder<T> where T : IDistributedApplicationComponent
{
    IDistributedApplicationBuilder ApplicationBuilder { get; }
    T Component { get; }
    IDistributedApplicationComponentBuilder<T> WithAnnotation<TAnnotation>() where TAnnotation : IDistributedApplicationComponentAnnotation, new() => WithAnnotation<TAnnotation>(new TAnnotation());
    IDistributedApplicationComponentBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : IDistributedApplicationComponentAnnotation;
}
