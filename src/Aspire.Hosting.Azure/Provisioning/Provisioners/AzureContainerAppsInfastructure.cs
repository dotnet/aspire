// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

// Logic to generate compute and networking infrastructure for Azure Container Apps
// based deployments.
internal class AzureContainerAppsInfastructure(DistributedApplicationExecutionContext executionContext)
{
    public async Task GenerateAdditionalInfrastructureAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // Add the compute infrastructure

        // Create the container app environment, and log analytics workspace
        var containerAppEnv = new AzureBicepResource("containerAppEnv", templateString:
            """
            param location string
            param tags object

            var resourceToken = uniqueString(resourceGroup().id)

            resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
              name: 'law-${resourceToken}'
              location: location
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
              tags: tags
            }

            resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
              name: 'cae-${resourceToken}'
              location: location
              properties: {
                appLogsConfiguration: {
                  destination: 'log-analytics'
                  logAnalyticsConfiguration: {
                    customerId: logAnalyticsWorkspace.properties.customerId
                    sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
                  }
                }
              }
              tags: tags
            }

            output id string = containerAppEnvironment.id
            output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
            output defaultDomain string = containerAppEnvironment.properties.defaultDomain
            """);

        containerAppEnv.Annotations.Add(new ManifestPublishingCallbackAnnotation(containerAppEnv.WriteToManifest));

        var containerAppEnvId = new BicepOutputReference("id", containerAppEnv);

        var containerRegistry = new AzureBicepResource("containerRegistry", templateString:
            """
            param location string
            param tags object
            param sku string = 'Basic'
            param adminUserEnabled bool = true

            var resourceToken = uniqueString(resourceGroup().id)
                    
            resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
                name: replace('acr${resourceToken}', '-', '')
                location: location
                sku: {
                    name: sku
                }
                properties: {
                    adminUserEnabled: adminUserEnabled
                }
                tags: tags
            }
                    
            resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
                name: 'mi-${resourceToken}'
                location: location
                tags: tags
            }

            resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
                scope: containerRegistry
                properties: {
                    principalId: managedIdentity.properties.principalId
                    principalType: 'ServicePrincipal'
                    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
                }
            }

            output mid string = managedIdentity.id
            output loginServer string = containerRegistry.properties.loginServer
            """);

        containerRegistry.Annotations.Add(new ManifestPublishingCallbackAnnotation(containerRegistry.WriteToManifest));

        var containerRegistryManagedIdentityId = new BicepOutputReference("mid", containerRegistry);
        var containerAppsRegistryUrl = new BicepOutputReference("loginServer", containerRegistry);

        var newResources = new List<IResource>
        {
            containerAppEnv,
            containerRegistry
        };

        var resourcesToRemove = new List<IResource>();

        // Inject keyvault resources
        foreach (var r in appModel.Resources.OfType<AzureBicepResource>())
        {
            if (r.Parameters.TryGetValue(AzureBicepResource.KnownParameters.KeyVaultName, out var value) &&
                value is null)
            {
                var kv = new AzureBicepResource(r.Name + "-kv", templateString:
                    """
                    param location string
                    param tags object

                    resource keyVault 'Microsoft.KeyVault/vaults@2022-02-01-preview' = {
                      name: 'kv-${uniqueString(resourceGroup().id)}'
                      location: location
                      properties: {
                        sku: {
                          family: 'A'
                          name: 'standard'
                        }
                        tenantId: subscription().tenantId
                        accessPolicies: []
                      }
                      tags: tags
                    }

                    output name string = keyVault.name
                    """);

                kv.Annotations.Add(new ManifestPublishingCallbackAnnotation(kv.WriteToManifest));

                newResources.Add(kv);

                r.Parameters[AzureBicepResource.KnownParameters.KeyVaultName] = new BicepOutputReference("name", kv);
            }
        }

        var domain = new BicepOutputReference("defaultDomain", containerAppEnv);
        var logAnalyticsWorkspaceId = new BicepOutputReference("logAnalyticsWorkspaceId", containerAppEnv);

        // Create a user assigned identity for all container apps
        // TODO: Make one per container app in the future
        var containerAppIdentity = new AzureBicepResource("default-identity", templateString:
            """
            param location string
            param tags object
                                              
            resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
                name: 'cai-${uniqueString(resourceGroup().id)}'
                location: location
                tags: tags
            }
                                                                                                                               
            output id string = identity.id
            output clientId string = identity.properties.clientId
            output principalId string = identity.properties.principalId
            """);

        containerAppIdentity.Annotations.Add(new ManifestPublishingCallbackAnnotation(containerAppIdentity.WriteToManifest));

        newResources.Add(containerAppIdentity);

        var identityId = new BicepOutputReference("id", containerAppIdentity);
        var clientId = new BicepOutputReference("clientId", containerAppIdentity);
        var principalId = new BicepOutputReference("principalId", containerAppIdentity);

        var containerAppEnviromentContext =
            new ContainerAppEnviromentContext(domain, identityId, containerAppsRegistryUrl, containerRegistryManagedIdentityId, principalId, clientId);

        foreach (var r in appModel.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!r.IsContainer() && r is not ProjectResource p)
            {
                continue;
            }

            var processingContext = await containerAppEnviromentContext.ProcessResourceAsync(r, executionContext, cancellationToken).ConfigureAwait(false);

            var containerAppIdParam = processingContext.AllocateParameter(containerAppEnvId);

            string? containerImageParam = null;

            if (r.TryGetContainerImageName(out var containerImageName))
            {
                containerImageParam = $"'{containerImageName}'";
            }
            else
            {
                processingContext.AllocateContainerRegistryParameters();

                containerImageParam = processingContext.AllocateParameter(new ContainerImage((ProjectResource)r));
            }

            var containerApp = new AzureBicepResource(r.Name + "-containerApp", templateString:
                $$$"""
                param location string
                param tags object = {}
                {{{processingContext.WriteParameters()}}}
                resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
                    name: '{{{r.Name.ToLowerInvariant()}}}'
                    location: location
                    tags: tags
                    {{{processingContext.WriteManagedIdentites()}}}
                    properties: {
                        environmentId: {{{containerAppIdParam}}}
                        configuration: {
                            activeRevisionsMode: 'Single'
                            {{{processingContext.WriteIngress()}}}
                            {{{processingContext.WriteContainerRegistryParameters()}}}
                            {{{processingContext.WriteSecrets()}}}
                        }
                        template: {
                            scale: {
                                minReplicas: {{{r.GetReplicaCount()}}}
                            }
                            containers: [
                                {
                                    image: {{{containerImageParam}}}
                                    name: '{{{r.Name}}}'
                                    {{{processingContext.WriteEnvironmentVariables()}}}
                                }
                            ]
                        }
                    }
                }
                """);

            if (r.IsContainer())
            {
                // We're going to re-write this container as a container app
                r.Annotations.Add(new ManifestPublishingCallbackAnnotation(containerApp.WriteToManifest));
            }
            else
            {
                containerApp.Annotations.Add(new ManifestPublishingCallbackAnnotation(containerApp.WriteToManifest));
                newResources.Add(containerApp);
            }

            foreach (var (key, value) in processingContext.Parameters)
            {
                // Inputs must exist on the resource that references them
                if (value is InputReference reference)
                {
                    containerApp.Annotations.Add(new InputAnnotation(reference.InputName, reference.Input.Secret)
                    {
                        Default = reference.Input.Default
                    });

                    var clone = new InputReference(containerApp, reference.InputName);
                    containerApp.Parameters[key] = clone;
                }
                else
                {
                    containerApp.Parameters[key] = value;
                }
            }
        }

        foreach (var r in newResources)
        {
            appModel.Resources.Add(r);
        }
    }

    // Trim a bicep expression ${x} to x
    private static string TrimExpression(string val)
    {
        if (val.StartsWith("${") && val.EndsWith('}'))
        {
            return val[2..^1];
        }

        return $"'{val}'";
    }

    // Process expressions in environment variables (or arguments)

    private sealed class ContainerImage(ProjectResource p) : IManifestExpressionProvider
    {
        public string ValueExpression => $"{{{p.Name}.containerImage}}";
    }

    private sealed class ContainerAppEnviromentContext(
        BicepOutputReference containerAppDomain,
        BicepOutputReference managedIdentityId,
        BicepOutputReference containerRegistryUrl,
        BicepOutputReference containerRegistryManagedIdentityId,
        BicepOutputReference principalId,
        BicepOutputReference clientId
        )
    {
        public BicepOutputReference ContainerAppDomain => containerAppDomain;
        public BicepOutputReference ManagedIdentityId => managedIdentityId;
        public BicepOutputReference ContainerRegistryUrl => containerRegistryUrl;
        public BicepOutputReference ContainerRegistryManagedIdentityId => containerRegistryManagedIdentityId;
        public BicepOutputReference PrincipalId => principalId;
        public BicepOutputReference ClientId => clientId;

        private readonly Dictionary<IResource, ProcessingContext> _processingContexts = [];

        public async Task<ProcessingContext> ProcessResourceAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            if (!_processingContexts.TryGetValue(resource, out var context))
            {
                _processingContexts[resource] = context = new ProcessingContext(resource, this);
                await context.ProcessResourceAsync(executionContext, cancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }

    private sealed class ProcessingContext(IResource resource, ContainerAppEnviromentContext containerAppEnviromentContext)
    {
        private readonly Dictionary<IManifestExpressionProvider, string> _allocatedParameters = [];
        private readonly ContainerAppEnviromentContext _containerAppEnviromentContext = containerAppEnviromentContext;

        private readonly Dictionary<string, (string Scheme, string Host, int Port, bool IsHttpIngress)> _endpointMapping = [];

        private (int Port, bool Http2, bool External)? _httpIngress;
        private readonly List<int> _additionalPorts = [];

        private string? _managedIdentityIdParameter;
        private string? _containerRegistryUrlParameter;
        private string? _containerRegistryManagedIdentityIdParameter;

        public IResource Resource => resource;

        // Set the parameters to add to the bicep file
        public Dictionary<string, IManifestExpressionProvider> Parameters { get; } = [];

        public Dictionary<string, (string, bool)> EnvironmentVariables { get; } = [];

        public Dictionary<string, AzureBicepResource> AzureDependencies { get; } = [];

        // ACA secrets
        public Dictionary<string, string> Secrets { get; } = [];

        public Task ProcessResourceAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            ProcessEndpoints();

            return ProcessEnvironmentAsync(executionContext, cancellationToken);
        }

        private void ProcessEndpoints()
        {
            if (!resource.TryGetEndpoints(out var endpoints) || !endpoints.Any())
            {
                return;
            }

            // Only http, https, and tcp are supported
            if (endpoints.Any(e => e.UriScheme is not ("tcp" or "http" or "https")))
            {
                throw new NotSupportedException("Supported endpoints are http, https, and tcp");
            }

            // First we group the endpoints by container port (aka destinations), this gives us the logical bindings or destinations
            var endpointsByContainerPort = endpoints.GroupBy(e => e.ContainerPort)
                                                    .Select(g => new
                                                    {
                                                        Port = g.Key,
                                                        Endpoints = g.ToArray(),
                                                        External = g.Any(e => e.IsExternal),
                                                        IsHttpOnly = g.All(e => e.UriScheme is "http" or "https"),
                                                        AnyH2 = g.Any(e => e.Transport is "http2")
                                                    })
                                                    .ToList();

            // Failure cases

            // Multiple external endpoints are not supported
            if (endpointsByContainerPort.Count(g => g.External) > 1)
            {
                throw new NotSupportedException("Multiple external endpoints are not supported");
            }

            // Any external non-http endpoints are not supported
            if (endpointsByContainerPort.Any(g => g.External && !g.IsHttpOnly))
            {
                throw new NotSupportedException("External non-HTTP(s) endpoints are not supported");
            }

            // Get all http only groups
            var httpOnlyEndpoints = endpointsByContainerPort.Where(g => g.IsHttpOnly).ToArray();

            // Do we only have one?
            var httpIngress = httpOnlyEndpoints.Length == 1 ? httpOnlyEndpoints[0] : null;

            if (httpIngress is null)
            {
                // We have more than one, pick prefer external one
                var externalHttp = httpOnlyEndpoints.Where(g => g.External).ToArray();

                if (externalHttp.Length == 1)
                {
                    httpIngress = externalHttp[0];
                }
                else if (httpOnlyEndpoints.Length > 0)
                {
                    // We have multiple HTTP only endpoints, we don't know which one should map to the ingress
                    // REVIEW: We could pick the first one, but that seems strange.
                    throw new NotSupportedException("Multiple internal only HTTP(s) endpoints are not supported.");
                }
            }

            if (httpIngress is not null)
            {
                // We're processed the http ingress, remove it from the list
                endpointsByContainerPort.Remove(httpIngress);

                var targetPort = httpIngress.Port ?? (resource is ProjectResource ? 8080 : 80);

                _httpIngress = (targetPort, httpIngress.AnyH2, httpIngress.External);

                foreach (var e in httpIngress.Endpoints)
                {
                    _endpointMapping[e.Name] = (e.UriScheme, resource.Name, targetPort, true);
                }
            }

            // ACA can't handle > 5 additional ports so throw if that's the case here
            if (endpointsByContainerPort.Count > 5)
            {
                throw new NotSupportedException("More than 5 additional ports are not supported. See https://learn.microsoft.com/en-us/azure/container-apps/ingress-overview#tcp for more details.");
            }

            foreach (var g in endpointsByContainerPort)
            {
                if (g.Port is null)
                {
                    throw new NotSupportedException("Container port is required for all endpoints");
                }

                _additionalPorts.Add(g.Port.Value);

                foreach (var e in g.Endpoints)
                {
                    _endpointMapping[e.Name] = (e.UriScheme, resource.Name, g.Port.Value, false);
                }
            }
        }

        private async Task ProcessEnvironmentAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var principalId = _containerAppEnviromentContext.PrincipalId;
            var clientId = _containerAppEnviromentContext.ClientId;

            if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
            {
                var context = new EnvironmentCallbackContext(executionContext, cancellationToken: cancellationToken);

                foreach (var c in environmentCallbacks)
                {
                    await c.Callback(context).ConfigureAwait(false);
                }

                foreach (var kv in context.EnvironmentVariables)
                {
                    var (val, isSecret) = await ProcessValueAsync(kv.Value, executionContext, cancellationToken).ConfigureAwait(false);

                    if (isSecret)
                    {
                        var secretName = kv.Key.Replace("__", "--").ToLowerInvariant();

                        Secrets[secretName] = val;

                        // The value is the secret name
                        val = secretName;
                    }

                    EnvironmentVariables[kv.Key] = (val, isSecret);
                }

                // Set the default managed identity client id if needed
                if (AzureDependencies.Count > 0)
                {
                    // TODO: Handle an existing AZURE_CLIENT_ID env set by the user
                    // TODO: Handle adding the user's managed identity to the container app

                    var requiresManagedIdentity = false;
                    foreach (var (_, resource) in AzureDependencies)
                    {
                        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalId, out var value) && value is null)
                        {
                            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalId] = principalId;
                            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalType] = "ServicePrincipal";
                            requiresManagedIdentity = true;
                        }
                    }

                    if (requiresManagedIdentity)
                    {
                        AllocateManagedIdentityIdParameter();

                        var parameterName = AllocateParameter(clientId);

                        EnvironmentVariables["AZURE_CLIENT_ID"] = ($"${{{parameterName}}}", false);
                    }
                }
            }
        }

        private async Task<(string, bool)> ProcessValueAsync(object value, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken, bool isSecret = false)
        {
            if (value is string s)
            {
                return (s, isSecret);
            }

            if (value is EndpointReference ep)
            {
                var context = ep.Resource == resource
                    ? this
                    : await _containerAppEnviromentContext.ProcessResourceAsync(ep.Resource, executionContext, cancellationToken).ConfigureAwait(false);

                var (scheme, host, port, isHttpIngress) = context._endpointMapping[ep.EndpointName];

                var url = isHttpIngress ? $"{scheme}://{host}" : $"{scheme}://{host}:{port}";

                return (url, isSecret);
            }

            if (value is ConnectionStringReference cs)
            {
                return await ProcessValueAsync(cs.Resource.ConnectionStringExpression, executionContext, cancellationToken, isSecret: true).ConfigureAwait(false);
            }

            if (value is IResourceWithConnectionString csrs)
            {
                return await ProcessValueAsync(csrs.ConnectionStringExpression, executionContext, cancellationToken, isSecret: true).ConfigureAwait(false);
            }

            if (value is ParameterResource param)
            {
                // This gets translated to a parameter 
                var parameterName = AllocateParameter(param);

                return ($"${{{parameterName}}}", param.Secret || isSecret);
            }

            if (value is BicepOutputReference output)
            {
                var parameterName = AllocateParameter(output);

                AzureDependencies[output.Resource.Name] = output.Resource;

                Parameters[parameterName] = output;

                return ($"${{{parameterName}}}", isSecret);
            }

            if (value is BicepSecretOutputReference secretOutputReference)
            {
                // Externalize secret outputs so azd can fill them in
                var parameterName = AllocateParameter(secretOutputReference);

                return ($"${{{parameterName}}}", true);
            }

            if (value is EndpointReferenceExpression epExpr)
            {
                var context = epExpr.Endpoint.Resource == resource
                    ? this
                    : await _containerAppEnviromentContext.ProcessResourceAsync(epExpr.Endpoint.Resource, executionContext, cancellationToken).ConfigureAwait(false);

                var (scheme, host, port, isHttpIngress) = context._endpointMapping[epExpr.Endpoint.EndpointName];

                return epExpr.Property switch
                {
                    EndpointProperty.Url => (isHttpIngress ? $"{scheme}://{host}" : $"{scheme}://{host}:{port}", isSecret),
                    EndpointProperty.Host or EndpointProperty.IPV4Host => (host, isSecret),
                    EndpointProperty.Port => (port.ToString(CultureInfo.InvariantCulture), isSecret),
                    EndpointProperty.Scheme => (scheme, isSecret),
                    _ => throw new NotSupportedException(),
                };
            }

            if (value is InputReference inputReference)
            {
                // Externalize input references
                var parameterName = AllocateParameter(inputReference);

                return ($"${{{parameterName}}}", inputReference.Input.Secret || isSecret);
            }

            if (value is ReferenceExpression expr)
            {
                var args = new object?[expr.ValueProviders.Count];
                var index = 0;
                var anySecrets = false;

                foreach (var vp in expr.ValueProviders)
                {
                    var (val, secret) = await ProcessValueAsync(vp, executionContext, cancellationToken, isSecret).ConfigureAwait(false);
                    args[index++] = val;

                    anySecrets = anySecrets || secret;
                }

                return (string.Format(CultureInfo.InvariantCulture, expr.Format, args), anySecrets);
            }

            throw new NotSupportedException("Unsupported value type " + value.GetType());
        }

        public string AllocateContainerAppsDomainParameter() =>
            AllocateParameter(_containerAppEnviromentContext.ContainerAppDomain);

        public void AllocateManagedIdentityIdParameter()
        {
            _managedIdentityIdParameter ??= AllocateParameter(_containerAppEnviromentContext.ManagedIdentityId);
        }

        public void AllocateContainerRegistryParameters()
        {
            _containerRegistryUrlParameter ??= AllocateParameter(_containerAppEnviromentContext.ContainerRegistryUrl);
            _containerRegistryManagedIdentityIdParameter ??= AllocateParameter(_containerAppEnviromentContext.ContainerRegistryManagedIdentityId);
        }

        public string AllocateParameter(IManifestExpressionProvider parameter)
        {
            if (!_allocatedParameters.TryGetValue(parameter, out var parameterName))
            {
                _allocatedParameters[parameter] = parameterName = $"param_{Parameters.Count}";
            }

            Parameters[parameterName] = parameter;
            return parameterName;
        }

        public string WriteIngress()
        {
            if (_httpIngress is null && _additionalPorts.Count == 0)
            {
                return "";
            }

            // Now we map the remainig endpoints. These should be internal only tcp/http based endpoints
            var sb = new StringBuilder();

            var skipAdditionalPort = 0;

            sb.AppendLine("ingress: {");
            if (_httpIngress is { } ingress)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  external: {ingress.External.ToString().ToLowerInvariant()}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  targetPort: {ingress.Port}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  transport: '{(ingress.Http2 ? "http2" : "http")}'");
            }
            else if (_additionalPorts.Count > 0)
            {
                // First port is the default

                var port = _additionalPorts[0];
                sb.AppendLine(CultureInfo.InvariantCulture, $"  external: false");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  targetPort: {port}");
                sb.AppendLine("  transport: 'tcp'");

                skipAdditionalPort++;
            }

            // Add additional ports
            // https://learn.microsoft.com/en-us/azure/container-apps/ingress-how-to?pivots=azure-cli#use-additional-tcp-ports
            var additionalPorts = _additionalPorts.Skip(skipAdditionalPort);
            if (_additionalPorts.Any())
            {
                sb.AppendLine("additionalPortMappings: [");
                foreach (var port in additionalPorts)
                {
                    sb.AppendLine("{");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  external: false");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  targetPort: {port}");
                    sb.AppendLine("}");
                }
                sb.AppendLine("]");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        public string WriteParameters()
        {
            var sb = new StringBuilder();
            foreach (var (name, val) in Parameters)
            {
                if (val is ParameterResource p && p.Secret || val is InputReference r && r.Input.Secret || val is BicepSecretOutputReference)
                {
                    sb.AppendLine("@secure()");
                }
                sb.AppendLine(CultureInfo.InvariantCulture, $"param {name} string // {val.ValueExpression}");
            }
            return sb.ToString();
        }

        public string WriteEnvironmentVariables()
        {
            if (EnvironmentVariables.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            sb.AppendLine("env: [");
            foreach (var kv in EnvironmentVariables)
            {
                var (val, isSecret) = kv.Value;

                if (isSecret)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{{ name: '{kv.Key}', secretRef: '{val}' }}");
                }
                else
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{{ name: '{kv.Key}', value: {TrimExpression(val)} }}");
                }
            }
            sb.AppendLine("]");
            return sb.ToString();
        }

        public string WriteSecrets()
        {
            if (Secrets.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.AppendLine("secrets: [");
            foreach (var kv in Secrets)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{{ name: '{kv.Key}', value: {TrimExpression(kv.Value)} }}");
            }
            sb.AppendLine("]");
            return sb.ToString();
        }

        internal string WriteManagedIdentites()
        {
            if (_managedIdentityIdParameter is null)
            {
                return "";
            }

            return
            $$"""
            identity: {
                type: 'UserAssigned'
                userAssignedIdentities: { '${{{_managedIdentityIdParameter}}}': {} }
            }
            """;
        }

        internal string WriteContainerRegistryParameters()
        {
            if (_containerRegistryUrlParameter is null)
            {
                return "";
            }

            return
            $$"""
            registries: [ {
                server: {{_containerRegistryUrlParameter}}
                identity: {{_containerRegistryManagedIdentityIdParameter}}
            } ]
            """;
        }
    }
}
