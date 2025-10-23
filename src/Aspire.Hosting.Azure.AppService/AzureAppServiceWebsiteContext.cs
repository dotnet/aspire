// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using System.Globalization;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Utils;
using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.OperationalInsights;
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

    // Naming the app service is globally unique (domain names), so we use the resource group ID to create a unique name
    // within the naming spec for the app service.
    public BicepValue<string> HostName => BicepFunction.Take(
        BicepFunction.Interpolate($"{BicepFunction.ToLower(resource.Name)}-{AzureAppServiceEnvironmentResource.GetWebSiteSuffixBicep()}"), 60);

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
            var context = new CommandLineArgsCallbackContext(Args, resource, cancellationToken)
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

        // App Service supports only one target port
        var targetPortEndpoints = endpoints.Where(e => e.IsExternal && e.TargetPort is not null).Select(e => e.TargetPort).Distinct().ToList();
        if (targetPortEndpoints.Count > 1)
        {
            throw new NotSupportedException("App Service does not support resources with multiple external endpoints.");
        }

        // Determine fallback target port if none is specified
        int? fallbackTargetPort = (resource, targetPortEndpoints) switch
        {
            // Project resources will fallback to container port if no target port is specified
            (ProjectResource, []) => null,
            // For other resources, default to port 8000 if no target port is specified
            (_, []) => 8000,
            // There's a target port, so no need for a fallback
            _ => null
        };

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
                TargetPort: endpoint.TargetPort ?? fallbackTargetPort,
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
            return (GetEndpointValue(context._endpointMapping[ep.EndpointName], EndpointProperty.Url), secretType);
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
            var val = GetEndpointValue(mapping, epExpr.Property);
            return (val, secretType);
        }

        if (value is ReferenceExpression expr)
        {
            if (expr.Format == "{0}" && expr.ValueProviders.Count == 1)
            {
                var val = ProcessValue(expr.ValueProviders[0], secretType, parent: parent);

                if (expr.StringFormats[0] is string format)
                {
                    val = (BicepFormattingHelpers.FormatBicepExpression(val, format), secretType);
                }

                return val;
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

                if (expr.StringFormats[index] is string format)
                {
                    val = BicepFormattingHelpers.FormatBicepExpression(val, format);
                }

                args[index++] = val;
            }

            return (FormattableStringFactory.Create(expr.Format, args), finalSecretType);
        }

        if (value is IManifestExpressionProvider manifestExpressionProvider)
        {
            return (AllocateParameter(manifestExpressionProvider, secretType), secretType);
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
            FormattableString fs => BicepFunction.Interpolate(fs),
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
            // Creating the app service with new sidecar configuration
            SiteConfig = new SiteConfigProperties()
            {
                LinuxFxVersion = "SITECONTAINERS",
                AcrUserManagedIdentityId = acrClientIdParameter,
                UseManagedIdentityCreds = true,
                // Setting NumberOfWorkers to maximum allowed value for Premium SKU
                // https://learn.microsoft.com/en-us/azure/app-service/manage-scale-up
                // This is required due to use of feature PerSiteScaling for the App Service plan
                // We want the web apps to scale normally as defined for the app service plan
                // so setting the maximum number of workers to the maximum allowed for Premium V2 SKU.
                NumberOfWorkers = 30,
                AppSettings = []
            },
            Identity = new ManagedServiceIdentity()
            {
                ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
                UserAssignedIdentities = []
            },
        };

        // Defining the main container for the app service
        var mainContainer = new SiteContainer("mainContainer")
        {
            Parent = webSite,
            Name = "main",
            Image = containerImage,
            AuthType = SiteContainerAuthType.UserAssigned,
            UserManagedIdentityClientId = acrClientIdParameter,
            IsMain = true
        };

        // There should be a single valid target port
        if (_endpointMapping.FirstOrDefault() is var  (_, mapping))
        {
            var targetPort = GetEndpointValue(mapping, EndpointProperty.TargetPort);

            mainContainer.TargetPort = targetPort;
            webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITES_PORT", Value = targetPort });
        }

        foreach (var kv in EnvironmentVariables)
        {
            var (val, secretType) = ProcessValue(kv.Value);
            var value = ResolveValue(val);

            if (secretType == SecretType.KeyVault)
            {
                // https://learn.microsoft.com/azure/app-service/app-service-key-vault-references?tabs=azure-cli#-understand-source-app-settings-from-key-vault
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

            mainContainer.StartUpCommand = Join(arrayExpression, " ");
        }

        infra.Add(mainContainer);

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

            // DefaultAzureCredential should only use ManagedIdentityCredential when running in Azure
            webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair
            {
                Name = "AZURE_TOKEN_CREDENTIALS",
                Value = "ManagedIdentityCredential"
            });
        }

        // Added appsetting to identify the resource in a specific aspire environment
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ASPIRE_ENVIRONMENT_NAME", Value = environmentContext.Environment.Name });

        // Probes
#pragma warning disable ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        if (resource.TryGetAnnotationsOfType<ProbeAnnotation>(out var probeAnnotations))
        {
            // AppService allow only one "health check" with only path, so prioritize "liveness" and/or take the first one
            var endpointProbeAnnotation = probeAnnotations
                .OfType<EndpointProbeAnnotation>()
                .OrderBy(probeAnnotation => probeAnnotation.Type == ProbeType.Liveness ? 0 : 1)
                .FirstOrDefault();

            if (endpointProbeAnnotation is not null)
            {
                webSite.SiteConfig.HealthCheckPath = endpointProbeAnnotation.Path;
            }
        }
#pragma warning restore ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        RoleAssignment? webSiteRa = null;
        if (environmentContext.Environment.EnableDashboard)
        {
            webSiteRa = AddDashboardPermissionAndSettings(webSite, acrClientIdParameter);
        }

        infra.Add(webSite);
        if (webSiteRa is not null)
        {
            infra.Add(webSiteRa);
        }

        if (environmentContext.Environment.EnableApplicationInsights)
        {
            EnableApplicationInsightsForWebSite(webSite);
        }

        // Allow users to customize the web app here
        if (resource.TryGetAnnotationsOfType<AzureAppServiceWebsiteCustomizationAnnotation>(out var customizeWebSiteAnnotations))
        {
            foreach (var customizeWebSiteAnnotation in customizeWebSiteAnnotations)
            {
                customizeWebSiteAnnotation.Configure(infra, webSite);
            }
        }
    }

    private BicepValue<string> GetEndpointValue(EndpointMapping mapping, EndpointProperty property)
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

        // https://learn.microsoft.com/azure/app-service/app-service-key-vault-references?tabs=azure-cli#-understand-source-app-settings-from-key-vault
        return secret.Properties.SecretUri;
    }

    private ProvisioningParameter AllocateParameter(IManifestExpressionProvider parameter, SecretType secretType = SecretType.None)
    {
        return parameter.AsProvisioningParameter(Infra, isSecure: secretType == SecretType.Normal);
    }

    private RoleAssignment AddDashboardPermissionAndSettings(WebSite webSite, ProvisioningParameter acrClientIdParameter)
    {
        var dashboardUri = environmentContext.Environment.DashboardUriReference.AsProvisioningParameter(Infra);
        var contributorId = environmentContext.Environment.WebsiteContributorManagedIdentityId.AsProvisioningParameter(Infra);
        var contributorPrincipalId = environmentContext.Environment.WebsiteContributorManagedIdentityPrincipalId.AsProvisioningParameter(Infra);

        // Add the appsettings specific to sending telemetry data to dashboard
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "OTEL_SERVICE_NAME", Value = resource.Name });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "OTEL_EXPORTER_OTLP_PROTOCOL", Value = "grpc" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "OTEL_EXPORTER_OTLP_ENDPOINT", Value = "http://localhost:6001" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITE_ENABLE_ASPIRE_OTEL_SIDECAR", Value = "true" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "OTEL_COLLECTOR_URL", Value = dashboardUri });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "OTEL_CLIENT_ID", Value = acrClientIdParameter });

        // Add Website Contributor role assignment to dashboard's managed identity for this webapp
        var websiteRaId = BicepFunction.GetSubscriptionResourceId(
                    "Microsoft.Authorization/roleDefinitions",
                    "de139f84-1756-47ae-9be6-808fbbe84772");
        var websiteRaName = BicepFunction.CreateGuid(webSite.Id, contributorId, websiteRaId);

        return new RoleAssignment(Infrastructure.NormalizeBicepIdentifier($"{Infra.AspireResource.Name}_ra"))
        {
            Name = websiteRaName,
            Scope = new IdentifierExpression(webSite.BicepIdentifier),
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal,
            PrincipalId = contributorPrincipalId,
            RoleDefinitionId = websiteRaId,
        };
    }

    private void EnableApplicationInsightsForWebSite(WebSite webSite)
    {
        // Create Log Analytics workspace
        var logAnalyticsWorkspace = new OperationalInsightsWorkspace("law_" + Infra.BicepName)// AspireResource.GetBicepIdentifier())
        {
            Sku = new OperationalInsightsWorkspaceSku()
            {
                Name = OperationalInsightsWorkspaceSkuName.PerGB2018
            }
        };

        Infra.Add(logAnalyticsWorkspace);

        // Create Application Insights resource linked to the Log Analytics workspace
        var applicationInsights = new ApplicationInsightsComponent("ai_" + Infra.BicepName)
        {
            ApplicationType = ApplicationInsightsApplicationType.Web,
            Kind = "web",
            WorkspaceResourceId = logAnalyticsWorkspace.Id,
            IngestionMode = ComponentIngestionMode.LogAnalytics
        };

        if (environmentContext.Environment.ApplicationInsightsLocation is not null)
        {
            var applicationInsightsLocation = new AzureLocation(environmentContext.Environment.ApplicationInsightsLocation);
            applicationInsights.Location = applicationInsightsLocation;
        }
        else if (environmentContext.Environment.ApplicationInsightsLocationParameter is not null)
        {
            var applicationInsightsLocationParameter = environmentContext.Environment.ApplicationInsightsLocationParameter.AsProvisioningParameter(Infra);
            applicationInsights.Location = applicationInsightsLocationParameter;
        }

        Infra.Add(applicationInsights);

        // Website configuration for Application Insights
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair
        {
            Name = "APPINSIGHTS_INSTRUMENTATIONKEY",
            Value = applicationInsights.InstrumentationKey
        });

        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair
        {
            Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
            Value = applicationInsights.ConnectionString
        });

        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair
        {
            Name = "ApplicationInsightsAgent_EXTENSION_VERSION",
            Value = "~3"
        });
    }

    enum SecretType
    {
        None,
        Normal,
        KeyVault
    }
}
