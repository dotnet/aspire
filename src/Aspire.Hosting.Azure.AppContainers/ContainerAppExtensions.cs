// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for customizing Azure Container App resource.
/// </summary>
public static class ContainerAppExtensions
{
    /// <summary>
    /// Configures the custom domain for the container app.
    /// </summary>
    /// <param name="app">The container app resource to configure for custom domain usage.</param>
    /// <param name="customDomain">A resource builder for a parameter resource capturing the name of the custom domain.</param>
    /// <param name="certificateName">A resource builder for a parameter resource capturing the name of the certficate configured in the Azure Portal.</param>
    /// <exception cref="ArgumentException">Throws if the container app resource is not parented to a <see cref="ResourceModuleConstruct"/>.</exception>
    /// <remarks>
    /// <para>The <see cref="ConfigureCustomDomain(ContainerApp, IResourceBuilder{ParameterResource}, IResourceBuilder{ParameterResource})"/> extension method
    /// simplifies the process of assigning a custom domain to a container app resource when it is deployed. It has no impact on local development.</para>
    /// <para>The <see cref="ConfigureCustomDomain(ContainerApp, IResourceBuilder{ParameterResource}, IResourceBuilder{ParameterResource})"/> method is used
    /// in conjunction with the <see cref="AzureContainerAppContainerExtensions.PublishAsAzureContainerApp{T}(IResourceBuilder{T}, Action{ResourceModuleConstruct, ContainerApp})"/>
    /// callback. Assigning a custom domain to a container app resource is a multi-step process and requires multiple deployments.</para>
    /// <para>The <see cref="ConfigureCustomDomain(ContainerApp, IResourceBuilder{ParameterResource}, IResourceBuilder{ParameterResource})"/> method takes
    /// two arguments which are parameter resource builders. The first is a parameter that represents the custom domain and the second is a parameter that
    /// represents the name of the managed certificate provisioned via the Azure Portal</para>
    /// <para>When deploying with custom domains configured for the first time leave the <paramref name="certificateName"/> parameter empty (when prompted
    /// by the Azure Developer CLI). Once the applicatio is deployed acucessfully access to the Azure Portal to bind the custom domain to a managed SSL
    /// certificate. Once the certificate is successfully provisioned, subsequent deployments of the application can use this certificate name when the
    /// <paramref name="certificateName"/> is prompted.</para>
    /// <para>For deployments triggered locally by the Azure Developer CLI the <c>config.json</c> file in the <c>.azure/{environment name}</c> path
    /// can by modified with the certificate name since Azure Developer CLI will not prompt again for the value.</para>
    /// </remarks>
    /// <example>
    /// This example shows declaring two parameters to capture the custom domain and certificate name and
    /// passing them to the <see cref="ConfigureCustomDomain(ContainerApp, IResourceBuilder{ParameterResource}, IResourceBuilder{ParameterResource})"/>
    /// method via the <see cref="AzureContainerAppContainerExtensions.PublishAsAzureContainerApp{T}(IResourceBuilder{T}, Action{ResourceModuleConstruct, ContainerApp})"/>
    /// extension method.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder();
    /// var customDomain = builder.AddParameter("customDomain"); // Value provided at first deployment.
    /// var certificateName = builder.AddParameter("certificateName"); // Value provided at second and subsequent deployments.
    /// builder.AddProject&lt;Projects.InventoryService&gt;("inventory")
    ///        .PublishAsAzureContainerApp((module, app) =>
    ///        {
    ///          app.ConfigureCustomDomain(customDomain, certificateName);
    ///        });
    /// </code>
    /// </example>
    public static void ConfigureCustomDomain(this ContainerApp app, IResourceBuilder<ParameterResource> customDomain, IResourceBuilder<ParameterResource> certificateName)
    {
        if (app.ParentInfrastructure is not ResourceModuleConstruct module)
        {
            throw new ArgumentException("Cannot configure custom domain when resource is not parented by ResourceModuleConstruct.", nameof(app));
        }

        var containerAppManagedEnvironmentIdParameter = module.GetResources().OfType<ProvisioningParameter>().Single(
            p => p.IdentifierName == "outputs_azure_container_apps_environment_id");
        var certificatNameParameter = certificateName.AsProvisioningParameter(module);
        var customDomainParameter = customDomain.AsProvisioningParameter(module);

        var bindingTypeConditional = new ConditionalExpression(
            new BinaryExpression(
                new IdentifierExpression(certificatNameParameter.IdentifierName),
                BinaryOperator.NotEqual,
                new StringLiteral(string.Empty)),
            new StringLiteral("SniEnabled"),
            new StringLiteral("Disabled")
            );

        var certificateOrEmpty = new ConditionalExpression(
            new BinaryExpression(
                new IdentifierExpression(certificatNameParameter.IdentifierName),
                BinaryOperator.NotEqual,
                new StringLiteral(string.Empty)),
            new InterpolatedString(
                "{0}/managedCertificates/{1}",
                [
                 new IdentifierExpression(containerAppManagedEnvironmentIdParameter.IdentifierName),
                    new IdentifierExpression(certificatNameParameter.IdentifierName)
                 ]),
            new NullLiteral()
            );

        app.Configuration.Value!.Ingress!.Value!.CustomDomains = new BicepList<ContainerAppCustomDomain>()
           {
                new ContainerAppCustomDomain()
                {
                    BindingType = bindingTypeConditional,
                    Name = new IdentifierExpression(customDomainParameter.IdentifierName),
                    CertificateId = certificateOrEmpty
                }
           };
    }
}
