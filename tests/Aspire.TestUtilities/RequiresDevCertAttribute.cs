// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Utils;
using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresDevCertAttribute : Attribute, ITraitAttribute
{
    // Returns true if a valid ASP.NET Core development certificate is found in the current user's certificate store.
    public static bool IsSupported => DevCertInStore();

    public string? Reason { get; init; }
    public RequiresDevCertAttribute(string? reason = null)
    {
        Reason = reason;
    }

    public static bool DevCertInStore()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        return store.Certificates
            .Where(c => c.IsAspNetCoreDevelopmentCertificate())
            .Where(c => c.NotAfter > DateTime.UtcNow)
            .OrderByDescending(c => c.GetCertificateVersion())
            .ThenByDescending(c => c.NotAfter)
            .Any();
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        if (!IsSupported)
        {
            return [new KeyValuePair<string, string>(XunitConstants.Category, "failing")];
        }

        return [];
    }
}
