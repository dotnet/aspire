// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Represents the status of an endpoint resolution operation.
/// </summary>
public readonly struct ResolutionStatus(ResolutionStatusCode statusCode, Exception? exception, string message) : IEquatable<ResolutionStatus>
{
    /// <summary>
    /// Indicates that resolution was not performed.
    /// </summary>
    public static readonly ResolutionStatus None = new(ResolutionStatusCode.None, exception: null, message: "");

    /// <summary>
    /// Indicates that resolution is ongoing and has not yet completed.
    /// </summary>
    public static readonly ResolutionStatus Pending = new(ResolutionStatusCode.Pending, exception: null, message: "Pending");

    /// <summary>
    /// Indicates that resolution has completed successfully.
    /// </summary>
    public static readonly ResolutionStatus Success = new(ResolutionStatusCode.Success, exception: null, message: "Success");

    /// <summary>
    /// Indicates that resolution was cancelled.
    /// </summary>
    public static readonly ResolutionStatus Cancelled = new(ResolutionStatusCode.Cancelled, exception: null, message: "Cancelled");

    /// <summary>
    /// Indicates that resolution did not find a result for the service.
    /// </summary>
    public static ResolutionStatus CreateNotFound(string message) => new(ResolutionStatusCode.NotFound, exception: null, message: message);

    /// <summary>
    /// Creates a status with a <see cref="StatusCode"/> equal to <see cref="ResolutionStatusCode.Error"/> with the provided exception.
    /// </summary>
    /// <param name="exception">The resolution exception.</param>
    /// <returns>A new <see cref="ResolutionStatus"/> instance.</returns>
    public static ResolutionStatus FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ResolutionStatus(ResolutionStatusCode.Error, exception, exception.Message);
    }

    /// <summary>
    /// Creates a status with a <see cref="StatusCode"/> equal to <see cref="ResolutionStatusCode.Pending"/> with the provided exception.
    /// </summary>
    /// <param name="exception">The resolution exception, if there was one.</param>
    /// <returns>A new <see cref="ResolutionStatus"/> instance.</returns>
    public static ResolutionStatus FromPending(Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ResolutionStatus(ResolutionStatusCode.Pending, exception, exception.Message);
    }

    /// <summary>
    /// Gets the resolution status code.
    /// </summary>
    public ResolutionStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// Gets the resolution exception.
    /// </summary>

    public Exception? Exception { get; } = exception;

    /// <summary>
    /// Gets the resolution status message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Compares the provided operands, returning <see langword="true"/> if they are equal and <see langword="false"/> if they are not equal.
    /// </summary>
    public static bool operator ==(ResolutionStatus left, ResolutionStatus right) => left.Equals(right);

    /// <summary>
    /// Compares the provided operands, returning <see langword="true"/> if they are not equal and <see langword="false"/> if they are equal.
    /// </summary>
    public static bool operator !=(ResolutionStatus left, ResolutionStatus right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ResolutionStatus status && Equals(status);

    /// <inheritdoc/>
    public bool Equals(ResolutionStatus other) => StatusCode == other.StatusCode &&
               EqualityComparer<Exception?>.Default.Equals(Exception, other.Exception) &&
               Message == other.Message;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(StatusCode, Exception, Message);

    public override string ToString() => Exception switch
    {
        not null => $"[{nameof(StatusCode)}: {StatusCode}, {nameof(Message)}: {Message}, {nameof(Exception)}: {Exception}]",
        _ => $"[{nameof(StatusCode)}: {StatusCode}, {nameof(Message)}: {Message}]"
    };
}
