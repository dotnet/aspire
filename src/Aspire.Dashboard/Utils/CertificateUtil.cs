// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Dashboard;

internal static class CertificateUtil
{
    public static X509CertificateCollection GetFileCertificate(string filePath, string? password, ILogger logger)
    {
        var certBytes = File.ReadAllBytes(filePath);

        var certContentType = X509Certificate2.GetCertContentType(certBytes);

        if (certContentType is X509ContentType.Pkcs12)
        {
            return [X509CertificateLoader.LoadPkcs12(certBytes, password)];
        }
        else
        {
            if (password is not null)
            {
                logger.LogDebug("Resource service certificate {FilePath} has type {Type} which does not support passwords, yet a password was configured. The certificate password will be ignored.", filePath, certContentType);
            }

            return [X509CertificateLoader.LoadCertificate(certBytes)];
        }
    }

    public static X509CertificateCollection GetKeyStoreCertificate(string subject, string? storeName = null, StoreLocation location = StoreLocation.CurrentUser)
    {
        storeName ??= "My";

        using var store = new X509Store(storeName: storeName, storeLocation: location);

        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, findValue: subject, validOnly: true);

        if (certificates is [])
        {
            throw new InvalidOperationException($"Unable to load client certificate with subject \"{subject}\" from key store.");
        }

        return certificates;
    }
}
