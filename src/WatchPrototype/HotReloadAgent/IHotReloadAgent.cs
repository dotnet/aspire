// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Abstraction for testing.
/// </summary>
internal interface IHotReloadAgent : IDisposable
{
    AgentReporter Reporter { get; }
    string Capabilities { get; }
    void ApplyManagedCodeUpdates(IEnumerable<RuntimeManagedCodeUpdate> updates);
    void ApplyStaticAssetUpdate(RuntimeStaticAssetUpdate update);
}
