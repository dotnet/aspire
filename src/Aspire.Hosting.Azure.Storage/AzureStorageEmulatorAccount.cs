// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a custom storage account for the Azure Storage Emulator (Azurite).
/// </summary>
/// <remarks>
/// <para>
/// This class allows defining custom storage accounts with unique names and keys
/// for use with the Azurite emulator. Multiple accounts can be configured to enable
/// scenarios such as:
/// </para>
/// <list type="bullet">
/// <item>Isolation between different parts of an application</item>
/// <item>Testing with multiple storage accounts without running multiple emulator containers</item>
/// <item>Separation of legacy and new table schemas</item>
/// </list>
/// </remarks>
public sealed class AzureStorageEmulatorAccount
{
    /// <summary>
    /// The default account name used by the Azure Storage Emulator.
    /// </summary>
    public const string DefaultAccountName = "devstoreaccount1";

    /// <summary>
    /// The default account key used by the Azure Storage Emulator (Base64 encoded).
    /// </summary>
    public const string DefaultAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    private static readonly Lazy<AzureStorageEmulatorAccount> s_default = new(() =>
        new AzureStorageEmulatorAccount(DefaultAccountName, DefaultAccountKey));

    /// <summary>
    /// Gets the default emulator account with the well-known credentials.
    /// </summary>
    public static AzureStorageEmulatorAccount Default => s_default.Value;

    /// <summary>
    /// Gets the account name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the account key (Base64 encoded).
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageEmulatorAccount"/> class.
    /// </summary>
    /// <param name="name">The account name.</param>
    /// <param name="key">The account key (Base64 encoded). If not provided, a new key will be generated.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public AzureStorageEmulatorAccount(string name, string? key = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        Key = key ?? GenerateAccountKey();
    }

    private static string GenerateAccountKey()
    {
        // Generate a 64-byte key (512 bits) for Azure Storage account key
        var keyBytes = new byte[64];
        RandomNumberGenerator.Fill(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }
}
