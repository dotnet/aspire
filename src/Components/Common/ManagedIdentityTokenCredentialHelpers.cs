// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Core;

namespace Aspire;

internal sealed class ManagedIdentityTokenCredentialHelpers
{
    public const string AzureDatabaseForPostgresSqlScope = "https://ossrdbms-aad.database.windows.net/.default";
    public const string AzureManagementScope = "https://management.azure.com/.default";

    public static readonly TokenRequestContext DatabaseForPostgresSqlTokenRequestContext = new([AzureDatabaseForPostgresSqlScope]);
    public static readonly TokenRequestContext ManagementTokenRequestContext = new([AzureManagementScope]);

    public static bool TryGetUsernameFromToken(string jwtToken, out string? username)
    {
        username = null;

        // Split the token into its parts (Header, Payload, Signature)
        var tokenParts = jwtToken.Split('.');
        if (tokenParts.Length != 3)
        {
            return false;
        }

        // The payload is the second part, Base64Url encoded
        var payload = tokenParts[1];

        // Add padding if necessary
        payload = AddBase64Padding(payload);

        // Decode the payload from Base64Url
        var decodedBytes = Convert.FromBase64String(payload);

        // Parse the decoded payload as JSON
        var reader = new Utf8JsonReader(decodedBytes);
        var payloadJson = JsonElement.ParseValue(ref reader);

        // Try to get the username from 'xms_mirid', 'upn', 'preferred_username', or 'unique_name' claims
        if (payloadJson.TryGetProperty("xms_mirid", out var xms_mirid) &&
            xms_mirid.GetString() is string xms_miridString &&
            ParsePrincipalName(xms_miridString) is string principalName)
        {
            username = principalName;
        }
        else if (payloadJson.TryGetProperty("upn", out var upn))
        {
            username = upn.GetString();
        }
        else if (payloadJson.TryGetProperty("preferred_username", out var preferredUsername))
        {
            username = preferredUsername.GetString();
        }
        else if (payloadJson.TryGetProperty("unique_name", out var uniqueName))
        {
            username = uniqueName.GetString();
        }

        return username != null;
    }

    // parse the xms_mirid claim which look like
    // /subscriptions/{subId}/resourcegroups/{resourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{principalName}
    private static string? ParsePrincipalName(string xms_mirid)
    {
        var lastSlashIndex = xms_mirid.LastIndexOf('/');
        if (lastSlashIndex == -1)
        {
            return null;
        }

        var beginning = xms_mirid.AsSpan(0, lastSlashIndex);
        var principalName = xms_mirid.AsSpan(lastSlashIndex + 1);

        if (principalName.IsEmpty || !beginning.EndsWith("providers/Microsoft.ManagedIdentity/userAssignedIdentities", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return principalName.ToString();
    }

    private static string AddBase64Padding(string base64) => (base64.Length % 4) switch
    {
        2 => base64 + "==",
        3 => base64 + "=",
        _ => base64,
    };
}
