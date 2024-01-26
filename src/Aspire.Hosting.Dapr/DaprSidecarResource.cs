// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Represents a Dapr sidecar resource.
/// </summary>
internal sealed class DaprSidecarResource(string name) : Resource(name), IDaprSidecarResource { }
