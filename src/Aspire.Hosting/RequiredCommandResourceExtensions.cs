// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding required command annotations to resources.
/// </summary>
public static class RequiredCommandResourceExtensions
{
    /// <summary>
    /// Declares that a resource requires a specific command/executable to be available on the local machine PATH before it can start.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="command">The command string (file name or path) that should be validated.</param>
    /// <param name="helpLink">An optional help link URL to guide users when the command is missing.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// The command is considered valid if either:
    /// 1. It is an absolute or relative path (contains a directory separator) that points to an existing file, or
    /// 2. It is discoverable on the current process PATH (respecting PATHEXT on Windows).
    /// If the command is not found, the resource will fail to start and an error message will be logged.
    /// </remarks>
    public static IResourceBuilder<T> WithRequiredCommand<T>(
        this IResourceBuilder<T> builder,
        string command,
        string? helpLink = null) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(command);

        builder.WithAnnotation(new RequiredCommandAnnotation(command)
        {
            HelpLink = helpLink
        });

        return builder;
    }

    /// <summary>
    /// Declares that a resource requires a specific command/executable to be available on the local machine PATH before it can start,
    /// with custom validation logic.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="command">The command string (file name or path) that should be validated.</param>
    /// <param name="validationCallback">A callback that validates the resolved command path.</param>
    /// <param name="helpLink">An optional help link URL to guide users when the command is missing or fails validation.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// The command is first resolved to a full path. If found, the validation callback is invoked with the resolved path.
    /// The callback should return a tuple indicating whether the command is valid and an optional validation message.
    /// If the command is not found or fails validation, the resource will fail to start.
    /// </remarks>
    public static IResourceBuilder<T> WithRequiredCommand<T>(
        this IResourceBuilder<T> builder,
        string command,
        Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>> validationCallback,
        string? helpLink = null) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(validationCallback);

        builder.WithAnnotation(new RequiredCommandAnnotation(command)
        {
            ValidationCallback = validationCallback,
            HelpLink = helpLink
        });

        return builder;
    }
}
