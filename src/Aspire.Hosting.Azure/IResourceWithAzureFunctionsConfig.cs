// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an resource that can provide configuration for Azure Functions.
/// </summary>
public interface IResourceWithAzureFunctionsConfig : IResource
{
    /// <summary>
    /// Applies the Azure Functions configuration to the target dictionary.
    /// </summary>
    /// <param name="target">The dictionary to which the Azure Functions configuration will be applied.</param>
    /// <param name="connectionName">The name of the connection key to be used for the given configuration.</param>
    void ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName);
}
