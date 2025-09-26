// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;

namespace Aspire.Hosting.Azure.AppService;

internal static class AzureAppServiceEnvironmentUtility
{
    const string ResourceName = "dashboard";

    public static BicepValue<string> DashboardHostName => BicepFunction.Take(
        BicepFunction.Interpolate($"{BicepFunction.ToLower(ResourceName)}-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}"), 60);

    public static WebSite AddDashboard(AzureResourceInfrastructure infra,
        UserAssignedIdentity otelIdentity,
        UserAssignedIdentity contributorIdentity,
        BicepValue<ResourceIdentifier> appServicePlanId)
    {
        var acrClientIdParameter = otelIdentity.ClientId;
        var contributorMidParameter = contributorIdentity.Id;
        var contributorClientIdParameter = contributorIdentity.ClientId;

        var webSite = new WebSite("webapp")
        {
            // Use the host name as the name of the web app
            Name = DashboardHostName,
            AppServicePlanId = appServicePlanId,
            // Aspire dashboards are created with a new kind aspiredashboard
            Kind = "app,linux,aspiredashboard",
            SiteConfig = new SiteConfigProperties()
            {
                LinuxFxVersion = "ASPIREDASHBOARD|1.0",
                AcrUserManagedIdentityId = acrClientIdParameter,
                UseManagedIdentityCreds = true,
                IsHttp20Enabled = true,
                Http20ProxyFlag = 1,
                // Setting NumberOfWorkers to 1 to ensure dashboard runs of 1 instance
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

        //var acrMid = BicepFunction.Interpolate($"{acrMidParameter}").Compile().ToString();
        //webSite.Identity.UserAssignedIdentities[acrMid] = new UserAssignedIdentityDetails();
        var contributorMid = BicepFunction.Interpolate($"{contributorMidParameter}").Compile().ToString();
        webSite.Identity.UserAssignedIdentities[contributorMid] = new UserAssignedIdentityDetails();

        // Security is handled by app service platform
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Frontend__AuthMode", Value = "Unsecured" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__AuthMode", Value = "Unsecured" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__ResourceServiceClient__AuthMode", Value = "Unsecured" });
        // Dashboard ports
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITES_PORT", Value = "5000" });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "HTTP20_ONLY_PORT", Value = "4317" });
        // Enable SCM preloading to ensure dashboard is always available
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITE_START_SCM_WITH_PRELOAD", Value = "true" });
        // Appsettings related to managed identity for auth
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "AZURE_CLIENT_ID", Value = contributorClientIdParameter });
        webSite.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ALLOWED_MANAGED_IDENTITIES", Value = acrClientIdParameter });
        infra.Add(webSite);

        return webSite;
    }
}
