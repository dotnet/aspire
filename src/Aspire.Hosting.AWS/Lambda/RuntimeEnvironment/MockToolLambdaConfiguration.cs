// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;

/// <summary>
///
/// </summary>
public sealed class MockToolLambdaConfiguration
{
    /// <summary>
    ///
    /// </summary>
    public bool Disabled { get; set; }
    /// <summary>
    ///
    /// </summary>
    public bool DisableLaunchWindow { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int Port { get; set; } = 5050;
}
