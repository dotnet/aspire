// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a destination for output data (stdout or stderr) from a process.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public abstract record ProcessOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessOutput"/> class.
    /// </summary>
    protected ProcessOutput() { }

    /// <summary>
    /// Gets an output destination that drains and discards output.
    /// This is the default when using <see cref="ICommand.Start"/>.
    /// </summary>
    public static ProcessOutput Null { get; } = NullOutput.s_instance;

    /// <summary>
    /// Gets an output destination that captures to a string in the <see cref="ProcessResult"/>.
    /// </summary>
    public static ProcessOutput Capture { get; } = CaptureOutput.s_instance;

    /// <summary>
    /// Drains and optionally captures output from the specified pipe reader.
    /// </summary>
    /// <param name="reader">The pipe reader to read from.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The captured output as a string, or null if not capturing.</returns>
    public abstract Task<string?> DrainAsync(PipeReader reader, CancellationToken ct);

    internal sealed record NullOutput : ProcessOutput
    {
        internal static readonly NullOutput s_instance = new();
        private NullOutput() { }

        /// <inheritdoc />
        public override async Task<string?> DrainAsync(PipeReader reader, CancellationToken ct)
        {
            // Drain and discard all output to prevent process blocking on full buffers
            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync(ct).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    // Consume all data (discard it)
                    reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }

            return null;
        }
    }

    internal sealed record CaptureOutput : ProcessOutput
    {
        internal static readonly CaptureOutput s_instance = new();
        private CaptureOutput() { }

        /// <inheritdoc />
        public override async Task<string?> DrainAsync(PipeReader reader, CancellationToken ct)
        {
            var capture = new StringBuilder();

            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync(ct).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    if (buffer.Length > 0)
                    {
                        foreach (var segment in buffer)
                        {
                            capture.Append(Encoding.UTF8.GetString(segment.Span));
                        }
                    }

                    reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }

            return capture.ToString().TrimEnd();
        }
    }

}
