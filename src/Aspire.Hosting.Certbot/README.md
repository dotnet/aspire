# Aspire.Hosting.Certbot library

Provides extension methods and resource definitions for an Aspire AppHost to configure a Certbot resource.

## Getting started

### Install the package

In your AppHost project, install the Aspire Certbot Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Certbot
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Certbot resource and consume the certificates:

```csharp
var domain = builder.AddParameter("domain");
var email = builder.AddParameter("email");

var certbot = builder.AddCertbot("certbot", domain, email)
    .WithHttp01Challenge();

var myService = builder.AddContainer("myservice", "myimage")
                       .WithCertbotCertificate(certbot);
```

## Certificate Locations

Certificates obtained by Certbot are stored at:

| Path | Description |
|------|-------------|
| `/etc/letsencrypt/live/{domain}/fullchain.pem` | The full certificate chain |
| `/etc/letsencrypt/live/{domain}/privkey.pem` | The private key |

These paths are accessible via the `CertbotResource.CertificatePath` and `CertbotResource.PrivateKeyPath` properties.



## Additional documentation

* https://certbot.eff.org/docs/
* https://letsencrypt.org/docs/

## Feedback & contributing

https://github.com/dotnet/aspire
