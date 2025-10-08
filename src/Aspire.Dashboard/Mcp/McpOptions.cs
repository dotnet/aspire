// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Aspire.Dashboard.Mcp;

public class McpOptions
{
    private byte[]? _primaryApiKeyBytes;
    private byte[]? _secondaryApiKeyBytes;

    public McpAuthMode? AuthMode { get; set; }
    public string Path { get; set; } = "/mcp";
    public string? PrimaryApiKey { get; set; }
    public string? SecondaryApiKey { get; set; }

    public byte[] GetPrimaryApiKeyBytes()
    {
        Debug.Assert(_primaryApiKeyBytes is not null, "Should have been parsed during validation.");
        return _primaryApiKeyBytes;
    }

    public byte[]? GetSecondaryApiKeyBytes() => _secondaryApiKeyBytes;

    internal bool TryParseOptions([NotNullWhen(false)] out string? errorMessage)
    {
        _primaryApiKeyBytes = PrimaryApiKey != null ? Encoding.UTF8.GetBytes(PrimaryApiKey) : null;
        _secondaryApiKeyBytes = SecondaryApiKey != null ? Encoding.UTF8.GetBytes(SecondaryApiKey) : null;

        errorMessage = null;
        return true;
    }
}
