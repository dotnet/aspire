// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Workload.Tests;

public sealed record ResourceRow(string Type, string Name, string State, string Source, string[] Endpoints);
