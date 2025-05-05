// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;

namespace Aspire.Hosting.Azure.AppService;

internal sealed class AzureAppServiceWebsiteContext(
    IResource resource,
    AzureAppServiceEnvironmentContext environmentContext)
{
    public IResource Resource => resource;

    record struct EndpointMapping(string Scheme, BicepValue<string> Host, int Port, int? TargetPort, bool IsHttpIngress, bool External);

    private readonly Dictionary<string, EndpointMapping> _endpointMapping = [];

    // Resolved environment variables and command line args
    // These contain the values that need to be further transformed into
    // bicep compatible values
    public Dictionary<string, object> EnvironmentVariables { get; } = [];
    public List<object> Args { get; } = [];

    private AzureResourceInfrastructure? _infrastructure;
    public AzureResourceInfrastructure Infra => _infrastructure ?? throw new InvalidOperationException("Infra is not set");

    // Naming the app service is globally unique (doman names), so we use the resource group ID to create a unique name
    // within the naming spec for the app service.
    public BicepValue<string> HostName => BicepFunction.Take(
        BicepFunction.Interpolate($"{BicepFunction.ToLower(resource.Name)}-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}"), 60);

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        ProcessEndpoints();

        await ProcessEnvironmentAsync(cancellationToken).ConfigureAwait(true);
        await ProcessArgumentsAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task ProcessEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(
                environmentContext.ExecutionContext, resource, EnvironmentVariables, cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(true);
            }
        }
    }

    private async Task ProcessArgumentsAsync(CancellationToken cancellationToken)
    {
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbackAnnotations))
        {
            var context = new CommandLineArgsCallbackContext(Args, cancellationToken)
            {
                ExecutionContext = environmentContext.ExecutionContext
            };

            foreach (var c in commandLineArgsCallbackAnnotations)
            {
                await c.Callback(context).ConfigureAwait(true);
            }
        }
    }

    private void ProcessEndpoints()
    {
        if (!resource.TryGetEndpoints(out var endpoints) || !endpoints.Any())
        {
            return;
        }

        // Only http/https are supported in App Service
        var unsupportedEndpoints = endpoints.Where(e => e.UriScheme is not ("http" or "https")).ToArray();
        if (unsupportedEndpoints.Length > 0)
        {
            throw new NotSupportedException($"The endpoint(s) {string.Join(", ", unsupportedEndpoints.Select(e => $"'{e.Name}'"))} on resource '{resource.Name}' specifies an unsupported scheme. Only http and https are supported in App Service.");
        }

        foreach (var endpoint in endpoints)
        {
            if (!endpoint.IsExternal)
            {
                throw new NotSupportedException($"The endpoint '{endpoint.Name}' on resource '{resource.Name}' is not external. App Service only supports external endpoints.");
            }

            // For App Service, we ignore port mappings since ports are handled by the platform
            _endpointMapping[endpoint.Name] = new(
                Scheme: endpoint.UriScheme,
                Host: HostName,
                Port: endpoint.UriScheme == "https" ? 443 : 80,
                TargetPort: null, // App Service manages internal port mapping
                IsHttpIngress: true,
                External: true); // All App Service endpoints are external
        }
    }

    private (object, SecretType) ProcessValue(object value, SecretType secretType = SecretType.None, object? parent = null)
    {
        if (value is string s)
        {
            return (s, secretType);
        }

        if (value is EndpointReference ep)
        {
            var context = environmentContext.GetAppServiceContext(ep.Resource);
            return (GetValue(context._endpointMapping[ep.EndpointName], EndpointProperty.Url), secretType);
        }

        if (value is ParameterResource param)
        {
            var st = param.Secret ? SecretType.Normal : secretType;
            return (AllocateParameter(param, secretType: st), st);
        }

        if (value is ConnectionStringReference cs)
        {
            return ProcessValue(cs.Resource.ConnectionStringExpression, secretType, parent);
        }

        if (value is IResourceWithConnectionString csrs)
        {
            return ProcessValue(csrs.ConnectionStringExpression, secretType, parent);
        }

        if (value is BicepOutputReference output)
        {
            return (AllocateParameter(output, secretType: secretType), secretType);
        }

        if (value is IAzureKeyVaultSecretReference vaultSecretReference)
        {
            if (parent is null)
            {
                return (AllocateKeyVaultSecretUriReference(vaultSecretReference), SecretType.KeyVault);
            }

            return (AllocateParameter(vaultSecretReference, secretType: SecretType.KeyVault), SecretType.KeyVault);
        }

        if (value is EndpointReferenceExpression epExpr)
        {
            var context = environmentContext.GetAppServiceContext(epExpr.Endpoint.Resource);
            var mapping = context._endpointMapping[epExpr.Endpoint.EndpointName];
            var val = GetValue(mapping, epExpr.Property);
            return (val, secretType);
        }

        if (value is ReferenceExpression expr)
        {
            if (expr.Format == "{0}" && expr.ValueProviders.Count == 1)
            {
                return ProcessValue(expr.ValueProviders[0], secretType, parent);
            }

            var args = new object[expr.ValueProviders.Count];
            var index = 0;
            var finalSecretType = SecretType.None;

            foreach (var vp in expr.ValueProviders)
            {
                var (val, secret) = ProcessValue(vp, secretType, expr);
                if (secret != SecretType.None)
                {
                    finalSecretType = SecretType.Normal;
                }
                args[index++] = val;
            }

            return (new BicepFormatString(expr.Format, args), finalSecretType);
        }

        throw new NotSupportedException($"Unsupported value type {value.GetType()}");
    }

    private static BicepValue<string> ResolveValue(object val)
    {
        return val switch
        {
            BicepValue<string> s => s,
            string s => s,
            ProvisioningParameter p => p,
            BicepFormatString fs => BicepFunction2.Interpolate(fs),
            _ => throw new NotSupportedException($"Unsupported value type {val.GetType()}")
        };
    }

    public void BuildWebSite(AzureResourceInfrastructure infra)
    {
        _infrastructure = infra;

        // We need to reference the container registry URL so that it exists in the manifest
        var containerRegistryUrl = environmentContext.Environment.ContainerRegistryUrl.AsProvisioningParameter(infra);
        var appServicePlanParameter = environmentContext.Environment.PlanIdOutputReference.AsProvisioningParameter(infra);
        var acrMidParameter = environmentContext.Environment.ContainerRegistryManagedIdentityId.AsProvisioningParameter(infra);
        var acrClientIdParameter = environmentContext.Environment.ContainerRegistryClientId.AsProvisioningParameter(infra);
        var containerImage = AllocateParameter(new ContainerImageReference(Resource));

        var webSite = new WebSite("webapp")
        {
            // Use the host name as the name of the web app
            Name = HostName,
            AppServicePlanId = appServicePlanParameter,
            SiteConfig = new SiteConfigProperties()
            {
                LinuxFxVersion = BicepFunction.Interpolate($"DOCKER|{containerImage}"),
                AcrUserManagedIdentityId = acrClientIdParameter,
                UseManagedIdentityCreds = true,
                AppSettings = []
            },
            Identity = new ManagedServiceIdentity()
            {
                ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
                UserAssignedIdentities = []
            },
        };

        foreach (var kv in EnvironmentVariables)
        {
            var (val, secretType) = ProcessValue(kv.Value);
            var value = ResolveValue(val);

            if (secretType == SecretType.KeyVault)
            {
                // https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references?tabs=azure-cli#-understand-source-app-settings-from-key-vault
                // @Microsoft.KeyVault({referenceString})
                value = BicepFunction.Interpolate($"@Microsoft.KeyVault(SecretUri={val})");
            }

            webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = kv.Key, Value = value });
        }

        if (Args.Count > 0)
        {
            var args = new List<BicepValue<string>>();

            foreach (var arg in Args)
            {
                var (val, secretType) = ProcessValue(arg);
                var value = ResolveValue(val);

                args.Add(value);
            }

            // App Service does not support array arguments, so we need to join them into a single string
            static FunctionCallExpression Join(BicepExpression args, string delimeter) =>
                new(new IdentifierExpression("join"), args, new StringLiteralExpression(delimeter));

            var arrayExpression = new ArrayExpression([.. args.Select(a => a.Compile())]);

            webSite.SiteConfig.AppCommandLine = Join(arrayExpression, " ");
        }

        var id = BicepFunction.Interpolate($"{acrMidParameter}").Compile().ToString();
        webSite.Identity.UserAssignedIdentities[id] = new UserAssignedIdentityDetails();

        // This is the user assigned identity associated with the web app, not the container registry
        if (resource.TryGetLastAnnotation<AppIdentityAnnotation>(out var appIdentityAnnotation))
        {
            var appIdentityResource = appIdentityAnnotation.IdentityResource;

            var computeIdentity = appIdentityResource.Id.AsProvisioningParameter(infra);

            var cid = BicepFunction.Interpolate($"{computeIdentity}").Compile().ToString();

            webSite.KeyVaultReferenceIdentity = computeIdentity;

            webSite.Identity.ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned;
            webSite.Identity.UserAssignedIdentities[cid] = new UserAssignedIdentityDetails();

            webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair
            {
                Name = "AZURE_CLIENT_ID",
                Value = appIdentityResource.ClientId.AsProvisioningParameter(infra)
            });
        }

        infra.Add(webSite);

        // Allow users to customize the web app here
        if (resource.TryGetAnnotationsOfType<AzureAppServiceWebsiteCustomizationAnnotation>(out var customizeWebSiteAnnotations))
        {
            foreach (var customizeWebSiteAnnotation in customizeWebSiteAnnotations)
            {
                customizeWebSiteAnnotation.Configure(infra, webSite);
            }
        }
    }

    private BicepValue<string> GetValue(EndpointMapping mapping, EndpointProperty property)
    {
        return property switch
        {
            EndpointProperty.Url => BicepFunction.Interpolate($"{mapping.Scheme}://{mapping.Host}.azurewebsites.net"),
            EndpointProperty.Host => BicepFunction.Interpolate($"{mapping.Host}.azurewebsites.net"),
            EndpointProperty.Port => mapping.Port.ToString(CultureInfo.InvariantCulture),
            EndpointProperty.TargetPort => mapping.TargetPort?.ToString(CultureInfo.InvariantCulture) ?? (BicepValue<string>)AllocateParameter(new ContainerPortReference(Resource)),
            EndpointProperty.Scheme => mapping.Scheme,
            EndpointProperty.HostAndPort => BicepFunction.Interpolate($"{mapping.Host}.azurewebsites.net"),
            EndpointProperty.IPV4Host => BicepFunction.Interpolate($"{mapping.Host}.azurewebsites.net"),
            _ => throw new NotSupportedException($"Unsupported endpoint property {property}")
        };
    }

    private BicepValue<string> AllocateKeyVaultSecretUriReference(IAzureKeyVaultSecretReference secretReference)
    {
        var secret = secretReference.AsKeyVaultSecret(Infra);

        // https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references?tabs=azure-cli#-understand-source-app-settings-from-key-vault
        return secret.Properties.SecretUri;
    }

    private ProvisioningParameter AllocateParameter(IManifestExpressionProvider parameter, SecretType secretType = SecretType.None)
    {
        return parameter.AsProvisioningParameter(Infra, isSecure: secretType == SecretType.Normal);
    }

    enum SecretType
    {
        None,
        Normal,
        KeyVault
    }
}
