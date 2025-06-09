// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Functions project resource within the Aspire hosting environment.
/// </summary>
/// <remarks>
/// This class is used to define and manage the configuration of an Azure Functions project,
/// including its associated host storage. We create a strongly-typed resource for the Azure Functions
/// to support Functions-specific customizations, like the mapping of connection strings and configurations
/// for host storage.
/// /// </remarks>
public class AzureFunctionsProjectResource(string name) : ProjectResource(name), IAzureFunctionsResource
{
    internal AzureStorageResource? HostStorage { get; set; }
}
