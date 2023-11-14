# Microsoft.Extensions.ServiceDiscovery.Dns

This library provides support for resolving service endpoints using DNS (Domain Name System). It provides two service endpoint resolvers:

- _DNS_, which resolves endpoints using DNS `A/AAAA` record queries. This means that it can resolve names to IP addresses, but cannot resolve port numbers endpoints. As such, port numbers are assumed to be the default for the protocol (for example, 80 for HTTP and 433 for HTTPS). The benefit of using the DNS resolver is that for cases where these default ports are appropriate, clients can spread their requests across hosts. For more information, see _Load-balancing with endpoint selectors_.

- _DNS SRV_, which resolves service names using DNS SRV record queries. This allows it to resolve both IP addresses and port numbers. This is useful for environments which support DNS SRV queries, such as Kubernetes (when configured accordingly).

## Resolving service endpoints with DNS

The _DNS_ resolver resolves endpoints using DNS `A/AAAA` record queries. This means that it can resolve names to IP addresses, but cannot resolve port numbers endpoints. As such, port numbers are assumed to be the default for the protocol (for example, 80 for HTTP and 433 for HTTPS). The benefit of using the DNS resolver is that for cases where these default ports are appropriate, clients can spread their requests across hosts. For more information, see _Load-balancing with endpoint selectors_.

To configure the DNS resolver in your application, add the DNS resolver to your host builder's service collection using the `AddDnsServiceEndPointResolver` method. service discovery as follows:

```csharp
builder.Services.AddServiceDiscoveryCore();
builder.Services.AddDnsServiceEndPointResolver();
```

## Resolving service endpoints in Kubernetes with DNS SRV

When deploying to Kubernetes, the DNS SRV service endpoint resolver can be used to resolve endpoints. For example, the following resource definition will result in a DNS SRV record being created for an endpoint named "default" and an endpoint named "dashboard", both on the service named "basket".

```yml
apiVersion: v1
kind: Service
metadata:
  name: basket
spec:
  selector:
    name: basket-service
  clusterIP: None
  ports:
  - name: default
    port: 8080
  - name: dashboard
    port: 8888
```

To configure a service to resolve the "dashboard" endpoint on the "basket" service, add the DNS SRV service endpoint resolver to the host builder as follows:

```csharp
builder.Services.AddServiceDiscoveryCore();
builder.Services.AddDnsSrvServiceEndPointResolver();
```

The special port name "default" is used to specify the default endpoint, resolved using the URI `http://basket`.

As in the previous example, add service discovery to an `HttpClient` for the basket service:

```csharp
builder.Services.AddHttpClient<BasketServiceClient>(
    static client => client.BaseAddress = new("http://basket"));
```

Similarly, the "dashboard" endpoint can be targeted as follows:

```csharp
builder.Services.AddHttpClient<BasketServiceDashboardClient>(
    static client => client.BaseAddress = new("http://_dashboard.basket"));
```

## Feedback & contributing

https://github.com/dotnet/aspire
