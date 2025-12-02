# Aspire.Hosting.Certbot library

Provides extension methods and resource definitions for an Aspire AppHost to configure a Certbot resource.

## Getting started

### Install the package

In your AppHost project, install the Aspire Certbot Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Certbot
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Certbot resource to obtain SSL/TLS certificates:

```csharp
var domain = builder.AddParameter("domain");
var email = builder.AddParameter("email");

var certbot = builder.AddCertbot("certbot", domain, email);

var myService = builder.AddContainer("myservice", "myimage")
                       .WithServerCertificates(certbot);
```

The certbot container will:

- Run in standalone mode to handle ACME challenges on port 80
- Obtain certificates for the specified domain using the ACME protocol
- Store certificates in a shared volume at `/etc/letsencrypt`
- Fix permissions so non-root containers can read the certificates

## Configuration

### Required Parameters

The Certbot resource requires two parameters:

| Parameter | Description |
|-----------|-------------|
| `domain` | The domain name to obtain a certificate for |
| `email` | The email address for certificate registration and notifications |

These parameters can be set via environment variables or configuration:

```bash
Parameters__domain=example.com
Parameters__email=admin@example.com
```

### Sharing Certificates with Other Resources

Use the `WithServerCertificates` extension method to mount the certificates volume in other containers:

```csharp
var yarp = builder.AddContainer("yarp", "myimage")
                  .WithServerCertificates(certbot);
```

Or mount the volume directly:

```csharp
var myService = builder.AddContainer("myservice", "myimage")
                       .WithVolume("letsencrypt", "/etc/letsencrypt");
```

### Certificate Locations

After Certbot obtains certificates, they are available at:

- Certificate: `/etc/letsencrypt/live/{domain}/fullchain.pem`
- Private Key: `/etc/letsencrypt/live/{domain}/privkey.pem`

The `CertbotResource` exposes these paths as `ReferenceExpression` properties that can be used to configure other resources:

```csharp
var certbot = builder.AddCertbot("certbot", domain, email);

// Access the certificate and private key paths
var certificatePath = certbot.Resource.CertificatePath;   // /etc/letsencrypt/live/{domain}/fullchain.pem
var privateKeyPath = certbot.Resource.PrivateKeyPath;     // /etc/letsencrypt/live/{domain}/privkey.pem
```

## Connection Properties

The Certbot resource does not expose connection properties through `WithReference`. This is because the Certbot resource is a certificate provisioning tool, not a service that other resources connect to.

Instead, use the `WithServerCertificates` extension method to share certificates with other containers via a mounted volume. See the [Sharing Certificates with Other Resources](#sharing-certificates-with-other-resources) section above for usage examples.

## Additional documentation

* https://certbot.eff.org/docs/
* https://letsencrypt.org/docs/

## Feedback & contributing

https://github.com/dotnet/aspire
