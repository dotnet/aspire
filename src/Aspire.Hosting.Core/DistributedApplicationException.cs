// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Represents an exception that is thrown when a distributed application error occurs.
/// </summary>
public class DistributedApplicationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationException"/> class.
    /// This represents an exception that is thrown when a distributed application error occurs.
    /// </summary>
    public DistributedApplicationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationException"/> class, 
    /// given the <paramref name="message"/>. This represents an exception that is thrown when 
    /// a distributed application error occurs.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public DistributedApplicationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationException"/> class, 
    /// given the <paramref name="message"/> and <paramref name="inner"/> exception. This 
    /// represents an exception that is thrown when a distributed application error occurs.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that caused the current exception.</param>
    public DistributedApplicationException(string message, Exception inner) : base(message, inner) { }
}
