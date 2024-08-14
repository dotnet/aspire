// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.AWS;

/// <summary>
/// Exception for errors provisioning AWS application resources
/// </summary>
/// <param name="message"></param>
/// <param name="innerException"></param>
public class AWSProvisioningException(string message, Exception? innerException = null)
    : Exception(ThrowIfNullOrEmpty(message), innerException)
{
    private static string ThrowIfNullOrEmpty(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
