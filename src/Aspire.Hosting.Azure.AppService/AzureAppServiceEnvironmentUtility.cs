// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;

namespace Aspire.Hosting.Azure.AppService;

internal static class AzureAppServiceEnvironmentUtility
{
    internal const string ResourceName = "aspiredashboard";

    public static BicepValue<string> GetDashboardHostName(string aspireResourceName, BicepValue<string>? deploymentSlot = null)
    {
        var baseExpr = BicepFunction.Interpolate($"{BicepFunction.ToLower(aspireResourceName)}-{BicepFunction.ToLower(ResourceName)}-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}");
        BicepValue<string> fullExpr = deploymentSlot is not null
            ? BicepFunction.Interpolate($"{BicepFunction.ToLower(aspireResourceName)}-{BicepFunction.ToLower(ResourceName)}-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}-{BicepFunction.ToLower(deploymentSlot)}")
            : baseExpr;

        return BicepFunction.Take(fullExpr, 60);
    }

    public static WebSite AddDashboard(
        AzureResourceInfrastructure infra,
        UserAssignedIdentity otelIdentity,
        BicepValue<ResourceIdentifier> appServicePlanId)
    {
        return AddDashboardCore<WebSite>(
            infra,
            otelIdentity,
            appServicePlanId,
            createDashboardResource: () => new WebSite("dashboard")
            {
                Name = GetDashboardHostName(infra.AspireResource.Name)
            }
        );
    }

    public static WebSiteSlot AddDashboardSlot(
        AzureResourceInfrastructure infra,
        UserAssignedIdentity otelIdentity,
        BicepValue<ResourceIdentifier> appServicePlanId,
        BicepValue<string> deploymentSlot)
    {
        WebSite dashboard = WebSite.FromExisting("dashboard");
        dashboard.Name = GetDashboardHostName(infra.AspireResource.Name);
        infra.Add(dashboard);

        return AddDashboardCore<WebSiteSlot>(
            infra,
            otelIdentity,
            appServicePlanId,
            createDashboardResource: () => new WebSiteSlot("dashboardSlot")
            {
                Name = deploymentSlot,
                Parent = dashboard
            },
            deploymentSlot: deploymentSlot
        );
    }

    private static T AddDashboardCore<T>(
        AzureResourceInfrastructure infra,
        UserAssignedIdentity otelIdentity,
        BicepValue<ResourceIdentifier> appServicePlanId,
        Func<T> createDashboardResource,
        Action<T>? configureExtra = null,
        BicepValue<string>? deploymentSlot = null)
        where T : ProvisionableResource
    {
        // This ACR identity is used by the dashboard to authorize the telemetry data
        // coming from the dotnet web apps. This identity is being assigned to every web app
        // in the aspire project and can be safely reused for authorization in the dashboard.
        var otelClientId = otelIdentity.ClientId;
        var prefix = infra.AspireResource.Name;
        var contributorIdentity = new UserAssignedIdentity(Infrastructure.NormalizeBicepIdentifier($"{prefix}-contributor-mi"));

        infra.Add(contributorIdentity);

        // Add Reader role assignment
        var rgRaId = BicepFunction.GetSubscriptionResourceId(
            "Microsoft.Authorization/roleDefinitions",
            "acdd72a7-3385-48ef-bd42-f606fba81ae7");
        var rgRaName = BicepFunction.CreateGuid(BicepFunction.GetResourceGroup().Id, contributorIdentity.Id, rgRaId);

        var rgRa = new RoleAssignment(Infrastructure.NormalizeBicepIdentifier($"{prefix}_ra"))
        {
            Name = rgRaName,
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal,
            PrincipalId = contributorIdentity.PrincipalId,
            RoleDefinitionId = rgRaId
        };

        infra.Add(rgRa);

        var dashboard = createDashboardResource();

        // Common configuration
        dynamic dashboardDynamic = dashboard;
        dashboardDynamic.AppServicePlanId = appServicePlanId;
        dashboardDynamic.Kind = "app,linux,aspiredashboard";
        dashboardDynamic.SiteConfig = new SiteConfigProperties()
        {
            LinuxFxVersion = "ASPIREDASHBOARD|1.0",
            AcrUserManagedIdentityId = otelClientId,
            UseManagedIdentityCreds = true,
            // Settings to enable HTTP/2
            IsHttp20Enabled = true,
            Http20ProxyFlag = 1,
            // Setting NumberOfWorkers to 1 to ensure dashboard runs on 1 instance
            NumberOfWorkers = 1,
            // IsAlwaysOn set to true ensures the app is always running
            IsAlwaysOn = true,
            AppSettings = []
        };
        dashboardDynamic.Identity = new ManagedServiceIdentity()
        {
            ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
            UserAssignedIdentities = []
        };

        var contributorMid = BicepFunction.Interpolate($"{contributorIdentity.Id}").Compile().ToString();
        dashboardDynamic.Identity.UserAssignedIdentities[contributorMid] = new UserAssignedIdentityDetails();

        // Common app settings
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Frontend__AuthMode", Value = "Unsecured" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__AuthMode", Value = "Unsecured" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__SuppressUnsecuredTelemetryMessage", Value = "true" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__ResourceServiceClient__AuthMode", Value = "Unsecured" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITES_PORT", Value = "5000" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "HTTP20_ONLY_PORT", Value = "4317" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITE_START_SCM_WITH_PRELOAD", Value = "true" });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "AZURE_CLIENT_ID", Value = contributorIdentity.ClientId });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ALLOWED_MANAGED_IDENTITIES", Value = otelClientId });
        dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ASPIRE_ENVIRONMENT_NAME", Value = infra.AspireResource.Name });

        // Slot-specific app setting
        if (deploymentSlot is not null)
        {
            dashboardDynamic.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ASPIRE_DEPLOYMENT_SLOT_NAME", Value = deploymentSlot });
        }

        configureExtra?.Invoke(dashboard);

        infra.Add(dashboard);

        // Outputs needed by the app service environment
        infra.Add(new ProvisioningOutput("AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_ID", typeof(string))
        {
            Value = contributorIdentity.Id
        });

        infra.Add(new ProvisioningOutput("AZURE_WEBSITE_CONTRIBUTOR_MANAGED_IDENTITY_PRINCIPAL_ID", typeof(string))
        {
            Value = contributorIdentity.PrincipalId
        });

        return dashboard;
    }
}
