// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Extension methods for <see cref="IProcessPipes"/>.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public static class ProcessPipesExtensions
{
    /// <summary>
    /// Writes bytes to the process's standard input.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="data">The bytes to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteAsync(
        this IProcessPipes process,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        await process.Input.WriteAsync(data, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes text to the process's standard input using UTF-8 encoding.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteAsync(
        this IProcessPipes process,
        ReadOnlyMemory<char> text,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var byteCount = Encoding.UTF8.GetByteCount(text.Span);
        var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var actualBytes = Encoding.UTF8.GetBytes(text.Span, bytes);
            await process.Input.WriteAsync(bytes.AsMemory(0, actualBytes), ct).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    /// <summary>
    /// Writes a line of text to the process's standard input using UTF-8 encoding.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="line">The line to write (newline will be appended).</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteLineAsync(
        this IProcessPipes process,
        string line,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(line);

        var lineWithNewline = line + Environment.NewLine;
        var bytes = Encoding.UTF8.GetBytes(lineWithNewline);
        await process.Input.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures the process completed successfully, throwing if it did not.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process did not complete successfully.
    /// </exception>
    public static async Task EnsureSuccessAsync(
        this IProcessPipes process,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var result = await process.WaitAsync(ct).ConfigureAwait(false);
        if (!result.Success)
        {
            var message = $"Process exited with code {result.ExitCode} (reason: {result.Reason})";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }
    }
}

/// <summary>
/// Extension methods for <see cref="IProcessLines"/>.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public static class ProcessLinesExtensions
{
    /// <summary>
    /// Ensures the process completed successfully, throwing if it did not.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process did not complete successfully.
    /// </exception>
    public static async Task EnsureSuccessAsync(
        this IProcessLines process,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var result = await process.WaitAsync(ct).ConfigureAwait(false);
        if (!result.Success)
        {
            var message = $"Process exited with code {result.ExitCode} (reason: {result.Reason})";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }
    }
}
