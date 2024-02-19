// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a callback to acquire a connection string. Used when resources swap underlying implementations.
/// </summary>
/// <param name="callback"></param>
public class ConnectionStringCallbackAnnotation(Func<string?> callback) : IResourceAnnotation
{
    /// <summary>
    /// Callback to acquire connection string.
    /// </summary>
    public Func<string?> Callback => callback;
}
