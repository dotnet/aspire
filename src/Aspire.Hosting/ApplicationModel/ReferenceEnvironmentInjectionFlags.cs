// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies which connection information should be injected into environment variables when <c>WithReference()</c> is invoked.
/// </summary>
[Flags]
public enum ReferenceEnvironmentInjectionFlags
{
    /// <summary>
    /// No connection information will be injected.
    /// </summary>
    None = 0,

    /// <summary>
    /// The connection string will be injected as an environment variable.
    /// </summary>
    ConnectionString = 1 << 0,

    /// <summary>
    /// Individual connection properties will be injected as environment variables.
    /// </summary>
    ConnectionProperties = 1 << 1,

    /// <summary>
    /// Both connection string and connection properties will be injected as environment variables.
    /// </summary>
    All = ConnectionString | ConnectionProperties
}
