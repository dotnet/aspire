// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Provides utilities for parsing connection strings to extract host and port information.
/// </summary>
public static class ConnectionStringParser
{
    private static readonly Dictionary<string, int> s_schemeDefaultPorts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["http"] = 80,
        ["https"] = 443,
        ["ftp"] = 21,
        ["ftps"] = 990,
        ["ssh"] = 22,
        ["telnet"] = 23,
        ["smtp"] = 25,
        ["dns"] = 53,
        ["dhcp"] = 67,
        ["tftp"] = 69,
        ["pop3"] = 110,
        ["ntp"] = 123,
        ["imap"] = 143,
        ["snmp"] = 161,
        ["ldap"] = 389,
        ["smtps"] = 465,
        ["ldaps"] = 636,
        ["imaps"] = 993,
        ["pop3s"] = 995,
        ["mssql"] = 1433,
        ["mysql"] = 3306,
        ["postgresql"] = 5432,
        ["postgres"] = 5432,
        ["redis"] = 6379,
        ["mongodb"] = 27017,
        ["amqp"] = 5672,
        ["amqps"] = 5671,
        ["kafka"] = 9092
    };

    private static readonly string[] s_hostAliases = ["host", "server", "data source", "addr", "address", "endpoint", "contact points"];

    private static readonly Regex s_hostPortRegex = new(@"(\[[^\]]+\]|[^,:;\s]+)[:|,](\d{1,5})", RegexOptions.Compiled);

    /// <summary>
    /// Attempts to extract a host and optional port from an arbitrary connection string.
    /// Returns <c>true</c> if a host could be identified; otherwise <c>false</c>.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <param name="host">When this method returns <c>true</c>, contains the host part with surrounding brackets removed; otherwise, an empty string.</param>
    /// <param name="port">When this method returns <c>true</c>, contains the explicit port, scheme-derived default, or <c>null</c> when unavailable; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a host was found; otherwise, <c>false</c>.</returns>
    public static bool TryDetectHostAndPort(
        string connectionString,
        [NotNullWhen(true)] out string? host,
        out int? port)
    {
        host = null;
        port = null;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        // 1. URI parse (including special handling for JDBC URLs)
        if (TryParseAsUri(connectionString, out var uriHost, out var uriPort))
        {
            host = uriHost;
            port = uriPort;
            return true;
        }

        // 2. Key-value scan
        var keyValuePairs = SplitIntoDictionary(connectionString);
        foreach (var hostAlias in s_hostAliases)
        {
            if (keyValuePairs.TryGetValue(hostAlias, out var token))
            {
                // First, check if the token is a complete URL
                if (TryParseAsUri(token, out var tokenHost, out var tokenPort))
                {
                    host = tokenHost;
                    port = tokenPort;
                    return true;
                }
                
                // Handle special case of multiple contact points (should return false)
                if (hostAlias.Equals("contact points", StringComparison.OrdinalIgnoreCase) && 
                    token.Contains(',') && token.Split(',').Length > 1)
                {
                    return false;
                }
                
                // Remove protocol prefixes like "tcp:", "udp:", etc. (but not from complete URLs)
                token = RemoveProtocolPrefix(token);
                
                if (token.Contains(',') || token.Contains(':'))
                {
                    var (hostPart, portPart) = SplitOnLast(token);
                    if (!string.IsNullOrEmpty(hostPart))
                    {
                        // Special handling for IPv6 addresses in brackets - don't split if already properly formatted
                        if (token.StartsWith('[') && token.Contains(']'))
                        {
                            var bracketEnd = token.IndexOf(']');
                            if (bracketEnd > 0)
                            {
                                host = TrimBrackets(token[..(bracketEnd + 1)]);
                                // Look for port after the bracket (could be colon or comma separated)
                                var afterBracket = token[(bracketEnd + 1)..];
                                if ((afterBracket.StartsWith(':') || afterBracket.StartsWith(',')) && afterBracket.Length > 1)
                                {
                                    port = ParseIntSafe(afterBracket[1..]) ?? PortFromKV(keyValuePairs);
                                }
                                else
                                {
                                    port = PortFromKV(keyValuePairs);
                                }
                                return true;
                            }
                        }
                        
                        host = TrimBrackets(hostPart);
                        port = ParseIntSafe(portPart) ?? PortFromKV(keyValuePairs);
                        return true;
                    }
                }
                else if (!string.IsNullOrEmpty(token))
                {
                    host = TrimBrackets(token);
                    port = PortFromKV(keyValuePairs);
                    return true;
                }
            }
        }

        // 3. Regex heuristic for host:port or host,port patterns
        var match = s_hostPortRegex.Match(connectionString);
        if (match.Success)
        {
            var hostPart = match.Groups[1].Value;
            var portPart = match.Groups[2].Value;
            if (!string.IsNullOrEmpty(hostPart))
            {
                host = TrimBrackets(hostPart);
                port = ParseIntSafe(portPart);
                return true;
            }
        }

        // 4. Looks like single host token (no '=' etc.)
        if (LooksLikeHost(connectionString))
        {
            host = TrimBrackets(connectionString);
            port = null;
            return true;
        }

        return false;
    }

    private static bool TryParseAsUri(string connectionString, [NotNullWhen(true)] out string? host, out int? port)
    {
        host = null;
        port = null;

        // Handle JDBC URLs specially since they're not recognized by Uri.TryCreate
        if (connectionString.StartsWith("jdbc:", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseJdbcUrl(connectionString, out host, out port);
        }

        // Standard URI parsing
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
        {
            host = TrimBrackets(uri.Host);
            port = uri.Port != -1 ? uri.Port : DefaultPortFromScheme(uri.Scheme);
            return true;
        }

        return false;
    }

    private static bool TryParseJdbcUrl(string jdbcUrl, [NotNullWhen(true)] out string? host, out int? port)
    {
        host = null;
        port = null;

        // JDBC URL pattern: jdbc:subprotocol://host:port/database
        var match = Regex.Match(jdbcUrl, @"^jdbc:[^:]+://([^:/\s]+)(?::(\d+))?(?:/.*)?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            host = match.Groups[1].Value;
            if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out var portValue))
            {
                port = portValue;
            }
            return true;
        }

        return false;
    }

    private static string TrimBrackets(string s) => s.Trim('[', ']');

    private static string RemoveProtocolPrefix(string value)
    {
        // Remove common protocol prefixes like "tcp:", "udp:", "ssl:", etc.
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var colonIndex = value.IndexOf(':');
        if (colonIndex > 0 && colonIndex < value.Length - 1)
        {
            var prefix = value[..colonIndex].ToLowerInvariant();
            // Only remove known protocol prefixes, not arbitrary single letters
            var knownProtocols = new[] { "tcp", "udp", "ssl", "tls", "http", "https", "ftp", "ssh" };
            if (knownProtocols.Contains(prefix))
            {
                return value[(colonIndex + 1)..];
            }
        }

        return value;
    }

    private static int? DefaultPortFromScheme(string? scheme)
    {
        if (string.IsNullOrEmpty(scheme))
        {
            return null;
        }

        return s_schemeDefaultPorts.TryGetValue(scheme, out var port) ? port : null;
    }

    private static int? PortFromKV(Dictionary<string, string> keyValuePairs)
    {
        return keyValuePairs.TryGetValue("port", out var portValue) ? ParseIntSafe(portValue) : null;
    }

    private static int? ParseIntSafe(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }

        if (int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
            value >= 0 && value <= 65535)
        {
            return value;
        }

        return null;
    }

    private static Dictionary<string, string> SplitIntoDictionary(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Split by semicolon first, then by whitespace if no semicolons found
        var parts = connectionString.Contains(';')
            ? connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            : connectionString.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            var equalIndex = trimmedPart.IndexOf('=');
            if (equalIndex > 0 && equalIndex < trimmedPart.Length - 1)
            {
                var key = trimmedPart[..equalIndex].Trim();
                var value = trimmedPart[(equalIndex + 1)..].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    private static (string host, string port) SplitOnLast(string token)
    {
        // Split on the last occurrence of ':' or ','
        var lastColonIndex = token.LastIndexOf(':');
        var lastCommaIndex = token.LastIndexOf(',');
        var splitIndex = Math.Max(lastColonIndex, lastCommaIndex);

        if (splitIndex > 0 && splitIndex < token.Length - 1)
        {
            return (token[..splitIndex].Trim(), token[(splitIndex + 1)..].Trim());
        }

        return (token, string.Empty);
    }

    private static bool LooksLikeHost(string connectionString)
    {
        // Simple heuristic: if it doesn't contain '=' and looks like a hostname or IP
        if (connectionString.Contains('='))
        {
            return false;
        }

        // Remove common file path indicators
        if (connectionString.StartsWith('/') || connectionString.StartsWith('\\') ||
            connectionString.StartsWith("./") || connectionString.StartsWith("../") ||
            (connectionString.Length > 2 && connectionString[1] == ':' && char.IsLetter(connectionString[0])))
        {
            return false;
        }

        // Should contain dots (for domains) or be a simple name, and not contain spaces
        var trimmed = connectionString.Trim();
        return !string.IsNullOrEmpty(trimmed) &&
               !trimmed.Contains(' ') &&
               (trimmed.Contains('.') || !trimmed.Contains('/'));
    }
}