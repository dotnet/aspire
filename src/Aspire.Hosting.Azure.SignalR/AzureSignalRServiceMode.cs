// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// SignalR service service modes
/// </summary>
public enum AzureSignalRServiceMode
{
    /// <summary>
    /// In default mode, both the client and the hub server connect to SignalR Service and use the service as a proxy
    /// </summary>
    Default,
    /// <summary>
    /// In Serverless mode, SignalR Service works with Azure Functions to provide real time messaging capability. Azure Function uses Azure SignalR triggers and bindings to handle messages from the SignalR clients.
    /// </summary>
    Serverless
}

