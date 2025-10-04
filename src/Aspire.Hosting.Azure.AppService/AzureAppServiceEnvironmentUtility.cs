// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;

namespace Aspire.Hosting.Azure.AppService;

internal static class AzureAppServiceEnvironmentUtility
{
    internal const string ResourceName = "dashboard";

    public static BicepValue<string> DashboardHostName => BicepFunction.Take(
        BicepFunction.Interpolate($"{BicepFunction.ToLower(ResourceName)}-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}"), 60);

    public static WebSite AddDashboard(AzureResourceInfrastructure infra,
        UserAssignedIdentity otelIdentity,
        BicepValue<ResourceIdentifier> appServicePlanId)
    {
        // This ACR identity is used by the dashboard to authorize the telemetry data
        // coming from the dotnet web apps. This identity is being assigned to every web app
        // in the aspire project and can be safely reused for authorization in the dashboard. 
        var otelClientId = otelIdentity.ClientId;
        var prefix = infra.AspireResource.Name;
        var contributorIdentity = new UserAssignedIdentity(Infrastructure.NormalizeBicepIdentifier($"{prefix}-contributor-mi"));
        
        infra.Add(contributorIdentity);

        // Add Website Contributor role assignment
        var rgRaId = BicepFunction.GetSubscriptionResourceId(
                    "Microsoft.Authorization/roleDefinitions",
                    "de139f84-1756-47ae-9be6-808fbbe84772");
        var rgRaName = BicepFunction.CreateGuid(BicepFunction.GetResourceGroup().Id, contributorIdentity.Id, rgRaId);
        var rgRa = new RoleAssignment(Infrastructure.NormalizeBicepIdentifier($"{prefix}_ra"))
        {
            Name = rgRaName,
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal,
            PrincipalId = contributorIdentity.PrincipalId,
            RoleDefinitionId = rgRaId,
        };

        infra.Add(rgRa);

        // Add Reader role assignment
        var rgRaId2 = BicepFunction.GetSubscriptionResourceId(
            "Microsoft.Authorization/roleDefinitions",
            "acdd72a7-3385-48ef-bd42-f606fba81ae7");
        var rgRaName2 = BicepFunction.CreateGuid(BicepFunction.GetResourceGroup().Id, contributorIdentity.Id, rgRaId2);

        var rgRa2 = new RoleAssignment(Infrastructure.NormalizeBicepIdentifier($"{prefix}_ra2"))
        {
            Name = rgRaName2,
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal,
            PrincipalId = contributorIdentity.PrincipalId,
            RoleDefinitionId = rgRaId2
        };

        infra.Add(rgRa2);

        var webSite = new WebSite("dashboard")
        {
            // Use the host name as the name of the web app
            Name = DashboardHostName,
            AppServicePlanId = appServicePlanId,
            // Aspire dashboards are created with a new kind aspiredashboard
            Kind = "app,linux,aspiredashboard",
            SiteConfig = new SiteConfigProperties()
            {
                LinuxFxVersion = "ASPIREDASHBOARD|1.0",
                AcrUserManagedIdentityId = otelClientId,
                UseManagedIdentityCreds = true,
                IsHttp20Enabled = true,
                Http20ProxyFlag = 1,
                // Setting NumberOfWorkers to 1 to ensure dashboard runs on 1 instance
                NumberOfWorkers = 1,
                // IsAlwaysOn set to true ensures the app is always running
                IsAlwaysOn = true,
                AppSettings = []
            },
            Identity = new ManagedServiceIdentity()
            {
                ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
                UserAssignedIdentities = []
            }
        };

        var contributorMid = BicepFunction.Interpolate($"{contributorIdentity.Id}").Compile().ToString();
        webSite.Identity.UserAssignedIdentities[contributorMid] = new UserAssignedIdentityDetails();

        // Security is handled by app service platform
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Frontend__AuthMode", Value = "Unsecured" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__AuthMode", Value = "Unsecured" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__SuppressUnsecuredTelemetryMessage", Value = "true" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__ResourceServiceClient__AuthMode", Value = "Unsecured" });
        // Dashboard ports
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITES_PORT", Value = "5000" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "HTTP20_ONLY_PORT", Value = "4317" });
        // Enable SCM preloading to ensure dashboard is always available
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITE_START_SCM_WITH_PRELOAD", Value = "true" });
        // Appsettings related to managed identity for auth
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "AZURE_CLIENT_ID", Value = contributorIdentity.ClientId });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ALLOWED_MANAGED_IDENTITIES", Value = otelClientId });
        infra.Add(webSite);

        return webSite;
    }
}
