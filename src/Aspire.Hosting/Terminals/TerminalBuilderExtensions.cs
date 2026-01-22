// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Extension methods for <see cref="Hex1bTerminalBuilder"/> to support Aspire terminal hosting.
/// </summary>
internal static class TerminalBuilderExtensions
{
    /// <summary>
    /// Configures the terminal to use a Unix domain socket workload adapter.
    /// </summary>
    /// <param name="builder">The terminal builder.</param>
    /// <param name="socketPath">The path for the Unix domain socket.</param>
    /// <param name="handle">
    /// When this method returns, contains the handle with socket path information.
    /// </param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public static Hex1bTerminalBuilder WithUdsWorkload(
        this Hex1bTerminalBuilder builder,
        string socketPath,
        out TerminalHostWorkloadHandle handle)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(socketPath);

        var adapter = new UdsWorkloadAdapter(socketPath);
        handle = new TerminalHostWorkloadHandle(socketPath, adapter);

        // Use the WithWorkload method to set the adapter
        builder.WithWorkload(adapter);

        return builder;
    }

    /// <summary>
    /// Configures the terminal to use a multiclient WebSocket presentation adapter.
    /// </summary>
    /// <param name="builder">The terminal builder.</param>
    /// <param name="adapter">
    /// When this method returns, contains the presentation adapter that can accept WebSocket connections.
    /// </param>
    /// <param name="width">Initial terminal width in columns.</param>
    /// <param name="height">Initial terminal height in rows.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public static Hex1bTerminalBuilder WithMulticlientWebSocket(
        this Hex1bTerminalBuilder builder,
        out MulticlientWebSocketPresentationAdapter adapter,
        int width = 80,
        int height = 24)
    {
        ArgumentNullException.ThrowIfNull(builder);

        adapter = new MulticlientWebSocketPresentationAdapter(width, height);
        builder.WithPresentation(adapter);

        return builder;
    }
}
