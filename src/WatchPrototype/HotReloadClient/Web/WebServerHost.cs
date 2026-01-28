// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.DotNet.HotReload;

internal sealed class WebServerHost(IDisposable listener, ImmutableArray<string> endPoints, string virtualDirectory) : IDisposable
{
    public ImmutableArray<string> EndPoints
        => endPoints;

    public string VirtualDirectory
        => virtualDirectory;

    public void Dispose()
        => listener.Dispose();
}
