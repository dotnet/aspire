// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Elastic.Clients.Elasticsearch;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Aspire.Elastic.Clients.Elasticsearch;

internal sealed class ElasticsearchOptions
{
    public string? Uri { get; private set; }

    public string? UserName { get; private set; }

    public string? Password { get; private set; }

    public string? CloudId { get; private set; }

    public string? CloudApiKey { get; private set; }

    public X509Certificate? Certificate { get; private set; }

    public bool AuthenticateWithBasicCredentials { get; private set; }

    public bool AuthenticateWithCertificate { get; private set; }

    public bool AuthenticateWithApiKey { get; private set; }

    public bool AuthenticateWithElasticCloud { get; private set; }

    public bool UseClusterHealthApi { get; set; }

    public string? ApiKey { get; private set; }

    public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool>? CertificateValidationCallback { get; private set; }

    public TimeSpan? RequestTimeout { get; set; }

    public ElasticsearchClient? Client { get; internal set; }

    public ElasticsearchOptions UseBasicAuthentication(string name, string password)
    {
        UserName = Guard.ThrowIfNull(name);
        Password = Guard.ThrowIfNull(password);

        CloudId = string.Empty;
        CloudApiKey = string.Empty;
        Certificate = null;
        AuthenticateWithApiKey = false;
        AuthenticateWithCertificate = false;
        AuthenticateWithElasticCloud = false;
        AuthenticateWithBasicCredentials = true;
        return this;
    }

    public ElasticsearchOptions UseCertificate(X509Certificate certificate)
    {
        Certificate = Guard.ThrowIfNull(certificate);

        UserName = string.Empty;
        Password = string.Empty;
        CloudId = string.Empty;
        CloudApiKey = string.Empty;
        AuthenticateWithApiKey = false;
        AuthenticateWithBasicCredentials = false;
        AuthenticateWithElasticCloud = false;
        AuthenticateWithCertificate = true;
        return this;
    }

    public ElasticsearchOptions UseApiKey(string apiKey)
    {
        ApiKey = Guard.ThrowIfNull(apiKey);

        UserName = string.Empty;
        Password = string.Empty;
        CloudId = string.Empty;
        CloudApiKey = string.Empty;
        Certificate = null;
        AuthenticateWithBasicCredentials = false;
        AuthenticateWithCertificate = false;
        AuthenticateWithElasticCloud = false;
        AuthenticateWithApiKey = true;

        return this;
    }

    public ElasticsearchOptions UseElasticCloud(string cloudId, string cloudApiKey)
    {
        CloudId = Guard.ThrowIfNull(cloudId);
        CloudApiKey = Guard.ThrowIfNull(cloudApiKey);

        UserName = string.Empty;
        Password = string.Empty;
        Certificate = null;
        AuthenticateWithBasicCredentials = false;
        AuthenticateWithCertificate = false;
        AuthenticateWithApiKey = false;
        AuthenticateWithElasticCloud = true;
        return this;
    }

    public ElasticsearchOptions UseServer(string uri)
    {
        Uri = Guard.ThrowIfNull(uri);

        return this;
    }

    public ElasticsearchOptions UseCertificateValidationCallback(Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
    {
        CertificateValidationCallback = callback;
        return this;
    }
}
