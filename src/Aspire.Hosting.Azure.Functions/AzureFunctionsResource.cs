// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <remarks>
/// This well-defined AzureFunctionsProjectResource type allows us to implement custom logic required by
/// Azure Functions. Specifically, running the `func host` via coretools to support running a
/// Functions host locally. We can imagine a future where we enable ProjectResource like behavior
/// for Functions apps, in which case we could model the function app as a project resource.
/// Regardless, we'll likely want to use a custom resource type for functions to permit us to
/// grow from one implementation to the next.
/// </remarks>
public class AzureFunctionsProjectResource(string name) : ProjectResource(name)
{
    internal AzureStorageResource? HostStorage { get; set; }
}
