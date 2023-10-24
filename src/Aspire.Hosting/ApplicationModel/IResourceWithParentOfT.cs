// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public interface IResourceWithParent<T> : IResource where T : IResource
{
    public T Parent { get; }
}
