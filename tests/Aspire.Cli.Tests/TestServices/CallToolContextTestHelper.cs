// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp.Tools;

namespace Aspire.Cli.Tests.TestServices;

/// <summary>
/// Provides helper methods for creating <see cref="CallToolContext"/> instances in tests.
/// </summary>
internal static class CallToolContextTestHelper
{
    /// <summary>
    /// Creates a <see cref="CallToolContext"/> for testing with optional arguments.
    /// </summary>
    /// <param name="arguments">Optional arguments to pass to the tool.</param>
    /// <returns>A new <see cref="CallToolContext"/> configured for testing.</returns>
    public static CallToolContext Create(IReadOnlyDictionary<string, JsonElement>? arguments = null)
    {
        return Create(new TestMcpNotifier(), arguments);
    }

    /// <summary>
    /// Creates a <see cref="CallToolContext"/> for testing with a specific notifier and optional arguments.
    /// </summary>
    /// <param name="notifier">The notifier to use.</param>
    /// <param name="arguments">Optional arguments to pass to the tool.</param>
    /// <returns>A new <see cref="CallToolContext"/> configured for testing.</returns>
    public static CallToolContext Create(TestMcpNotifier notifier, IReadOnlyDictionary<string, JsonElement>? arguments = null)
    {
        return new CallToolContext
        {
            Notifier = notifier,
            McpClient = null,
            Arguments = arguments
        };
    }
}
