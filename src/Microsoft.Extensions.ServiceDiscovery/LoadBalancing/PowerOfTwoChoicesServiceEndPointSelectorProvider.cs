// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Provides instances of <see cref="PowerOfTwoChoicesServiceEndPointSelector"/>.
/// </summary>
public class PowerOfTwoChoicesServiceEndPointSelectorProvider : IServiceEndPointSelectorProvider
{
    /// <summary>
    /// Gets a shared instance of this class.
    /// </summary>
    public static PowerOfTwoChoicesServiceEndPointSelectorProvider Instance { get; } = new();

    /// <inheritdoc/>
    public IServiceEndPointSelector CreateSelector() => new PowerOfTwoChoicesServiceEndPointSelector();
}
