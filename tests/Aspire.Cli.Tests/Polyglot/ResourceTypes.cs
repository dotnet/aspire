// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Polyglot;
using PolyglotIgnore = global::Aspire.Hosting.Polyglot.PolyglotLanguages;

namespace Aspire.Cli.Tests.Polyglot;

public class ContainerResource : Resource
{
    public ContainerResource(string name) : base(name)
    {
    }
}

public static class ContainerResourceBuilderExtensions
{
    // e.g., AddRedis
    public static IResourceBuilder<ContainerResource> AddSomeResource(this IDistributedApplicationBuilder builder) => throw new NotImplementedException();

    // e.g., WithVolume
    public static IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, string? name, string target, bool isReadOnly = false) where T : ContainerResource => throw new NotImplementedException();

    // e.g., WithRedisCommander
    public static IResourceBuilder<ContainerResource> WithSomethingSpecial<T>(this IResourceBuilder<ContainerResource> builder, string? name, string target, bool isReadOnly = false) => throw new NotImplementedException();

    [PolyglotIgnore(Reason = "", Languages = PolyglotIgnore.All)]
    public static IResourceBuilder<T> Ignored<T>(this IResourceBuilder<T> builder, string? name, string target, bool isReadOnly = false) where T : ContainerResource => throw new NotImplementedException();
}

public class DistributedApplicationBuilder<T> where T : IResource
{
    public IResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : class, new() => throw new NotImplementedException();
}
