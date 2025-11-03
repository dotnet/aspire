// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents a node application.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="command">The command to execute.</param>
/// <param name="workingDirectory">The working directory to use for the command.</param>
public class NodeAppResource(string name, string command, string workingDirectory)
    : JavaScriptAppResource(name, command, workingDirectory), IResourceWithServiceDiscovery;
