// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

/// <summary>
/// Provides a method to validate a connection string.
/// </summary>
public static class ConnectionStringValidation
{
    /// <summary>
    /// Validates the provided connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to validate.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="defaultConfigSectionName">The default configuration section name.</param>
    /// <param name="typeSpecificSectionName">The type-specific configuration section name (optional).</param>
    /// <param name="isEfDesignTime">Indicates if it is EF design time (optional).</param>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is missing and it is not EF design time.</exception>
    public static void ValidateConnectionString(string? connectionString, string connectionName, string defaultConfigSectionName, string? typeSpecificSectionName = null, bool isEfDesignTime = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString) && !isEfDesignTime)
        {
            var errorMessage = (!string.IsNullOrEmpty(typeSpecificSectionName))
                ? $"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{defaultConfigSectionName}' or '{typeSpecificSectionName}' configuration section."
                : $"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{defaultConfigSectionName}' configuration section.";

            throw new InvalidOperationException(errorMessage);
        }
    }
}
