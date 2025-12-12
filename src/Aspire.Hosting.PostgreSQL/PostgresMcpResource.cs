// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

/// <summary>
/// A resource representing the PostgreSQL MCP sidecar.
/// </summary>
public class PostgresMcpResource(string name) : ContainerResource(name);
