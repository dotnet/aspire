// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.DebugAdapter.Types;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Exception thrown when a debug adapter returns an error response.
/// </summary>
public class DebugAdapterException : Exception
{
    /// <summary>
    /// The command that failed.
    /// </summary>
    public string? Command { get; }

    /// <summary>
    /// The error message from the adapter.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// The detailed error body, if provided by the adapter.
    /// </summary>
    public ErrorResponse? ErrorBody { get; }

    /// <summary>
    /// The full response message that indicated failure.
    /// </summary>
    public ResponseMessage Response { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugAdapterException"/> class.
    /// </summary>
    /// <param name="response">The failed response message.</param>
    public DebugAdapterException(ResponseMessage response)
        : base(FormatMessage(response))
    {
        Response = response;
        Command = response.CommandName;
        ErrorMessage = response.Message;

        // Try to extract ErrorResponse body if present
        if (response is { Body: ErrorResponse errorResponse })
        {
            ErrorBody = errorResponse;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugAdapterException"/> class with an inner exception.
    /// </summary>
    /// <param name="response">The failed response message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DebugAdapterException(ResponseMessage response, Exception innerException)
        : base(FormatMessage(response), innerException)
    {
        Response = response;
        Command = response.CommandName;
        ErrorMessage = response.Message;

        if (response is { Body: ErrorResponse errorResponse })
        {
            ErrorBody = errorResponse;
        }
    }

    private static string FormatMessage(ResponseMessage response)
    {
        var command = response.CommandName ?? "unknown";
        var message = response.Message ?? "Unknown error";
        return $"Debug adapter request '{command}' failed: {message}";
    }
}
