// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Marker interface for resources that can have an interactive terminal attached.
/// </summary>
public interface IResourceWithTerminal : IResource
{
}
