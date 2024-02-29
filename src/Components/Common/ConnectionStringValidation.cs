// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

internal static class ConnectionStringValidation
{
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
