// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The exception that is thrown when a parameter resource cannot be initialized because its value is missing or cannot be resolved.
/// </summary>
/// <remarks>
/// This exception is typically thrown when:
/// <list type="bullet">
/// <item><description>A parameter value is not provided in configuration and has no default value</description></item>
/// <item><description>A parameter's value callback throws an exception during execution</description></item>
/// <item><description>A parameter's value cannot be retrieved from the configured source (e.g., user secrets, environment variables)</description></item>
/// </list>
/// </remarks>
public class MissingParameterValueException : DistributedApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingParameterValueException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MissingParameterValueException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingParameterValueException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public MissingParameterValueException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
