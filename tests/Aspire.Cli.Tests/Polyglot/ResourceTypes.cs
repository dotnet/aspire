// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Tests.Polyglot;

public interface IResource { }
public class Resource : IResource { }
public interface IResourceBuilder<out T> where T : IResource { }

public class ContainerResource : Resource { }

public static class ContainerResourceBuilderExtensions
{
    public static IResourceBuilder<T> WithVolume<T>(this IResourceBuilder<T> builder, string? name, string target, bool isReadOnly = false) where T : ContainerResource => throw new NotImplementedException();
}

public class DistributedApplicationBuilder<T> where T : IResource
{
    public IResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation) where TAnnotation : class, new() => throw new NotImplementedException();
}
