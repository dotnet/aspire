// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the result of validating a required command.
/// </summary>
public sealed class RequiredCommandValidationResult
{
    private RequiredCommandValidationResult(bool isValid, string? validationMessage)
    {
        IsValid = isValid;
        ValidationMessage = validationMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the command validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets an optional validation message describing why validation failed.
    /// </summary>
    public string? ValidationMessage { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static RequiredCommandValidationResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed validation result with the specified message.
    /// </summary>
    /// <param name="message">A message describing why validation failed.</param>
    /// <returns>A failed validation result.</returns>
    public static RequiredCommandValidationResult Failure(string message) => new(false, message);
}
