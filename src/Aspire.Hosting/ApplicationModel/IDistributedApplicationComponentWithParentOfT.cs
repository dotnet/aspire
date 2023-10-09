// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public interface IDistributedApplicationComponentWithParent<T> : IDistributedApplicationComponent where T : IDistributedApplicationComponent
{
    public T Parent { get; }
    public string? GetConnectionString();
}
