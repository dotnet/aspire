// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IOutgoingPeerResolver
{
    bool TryResolvePeerName(KeyValuePair<string, string>[] attributes, out string? name, out ResourceViewModel? matchedResourced);
    IDisposable OnPeerChanges(Func<Task> callback);
}
