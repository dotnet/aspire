// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// 
/// </summary>
public interface IResourceReferenceable<in TSource, in TDestination> : IResource
    where TSource : IResource
    where TDestination : IResource
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    static abstract void ProcessReference(IResourceBuilder<TSource> source, IResourceBuilder<TDestination> destination);

}
