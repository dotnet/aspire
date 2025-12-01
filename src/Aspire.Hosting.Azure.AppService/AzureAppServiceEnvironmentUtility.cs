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

    public static BicepValue<string> GetDashboardHostName(string aspireResourceName)
    {
        return BicepFunction.Take(
    BicepFunction.Interpolate($"{BicepFunction.ToLower(aspireResourceName)}-{BicepFunction.ToLower(ResourceName)}-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}"), 60);
    }

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

        var dashboard = new WebSite("dashboard")
        {
            // Use the host name as the name of the web app
            Name = GetDashboardHostName(infra.AspireResource.Name),
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
                // Setting instance count to 1 to ensure dashboard runs on 1 instance
                NumberOfWorkers = 1,
                FunctionAppScaleLimit = 1,
                ElasticWebAppScaleLimit = 1,
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
        dashboard.Identity.UserAssignedIdentities[contributorMid] = new UserAssignedIdentityDetails();

        // Security is handled by app service platform
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Frontend__AuthMode", Value = "Unsecured" });
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__AuthMode", Value = "Unsecured" });
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__Otlp__SuppressUnsecuredTelemetryMessage", Value = "true" });
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "Dashboard__ResourceServiceClient__AuthMode", Value = "Unsecured" });
        // Dashboard ports
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITES_PORT", Value = "5000" });
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "HTTP20_ONLY_PORT", Value = "4317" });
        // Enable SCM preloading to ensure dashboard is always available
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "WEBSITE_START_SCM_WITH_PRELOAD", Value = "true" });
        // Appsettings related to managed identity for auth
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "AZURE_CLIENT_ID", Value = contributorIdentity.ClientId });
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ALLOWED_MANAGED_IDENTITIES", Value = otelClientId });
        // Added appsetting to identify the resources in a specific aspire environment
        dashboard.SiteConfig.AppSettings.Add(new AppServiceNameValuePair { Name = "ASPIRE_ENVIRONMENT_NAME", Value = infra.AspireResource.Name });

        infra.Add(dashboard);

        // Outputs needed by the app service environment
        // This identity needs website contributor access on the websites for resource server to work
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

    public static void AddAzurePlaywrightWorkspaceResource(AzureResourceInfrastructure infra, AzureAppServiceEnvironmentResource resource, string prefix)
    {
        var playwrightWorkspaceResourceName = Infrastructure.NormalizeBicepIdentifier($"{prefix}_playwright");
        
        var playwrightWorkspace = new PlaywrightWorkspaceResource(playwrightWorkspaceResourceName);

        // Set location if specified
        if (resource.PlaywrightWorkspaceLocation is not null)
        {
            playwrightWorkspace.Location = new AzureLocation(resource.PlaywrightWorkspaceLocation);
        }
        else if (resource.PlaywrightWorkspaceLocationParameter is not null)
        {
            var locationParameter = resource.PlaywrightWorkspaceLocationParameter.AsProvisioningParameter(infra);
            playwrightWorkspace.Location = locationParameter;
        }

        infra.Add(playwrightWorkspace);

        infra.Add(new ProvisioningOutput("AZURE_PLAYWRIGHT_WORKSPACE_NAME", typeof(string))
        {
            Value = new MemberExpression(new IdentifierExpression(playwrightWorkspace.BicepIdentifier), "name")
        });

        infra.Add(new ProvisioningOutput("AZURE_PLAYWRIGHT_WORKSPACE_ID", typeof(string))
        {
            Value = new MemberExpression(new IdentifierExpression(playwrightWorkspace.BicepIdentifier), "id")
        });
    }
}

/// <summary>
/// Represents an Azure Playwright Testing Workspace resource.
/// </summary>
internal sealed class PlaywrightWorkspaceResource : ProvisionableResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaywrightWorkspaceResource"/> class.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier for this resource.</param>
    internal PlaywrightWorkspaceResource(string bicepIdentifier)
        : base(bicepIdentifier, new("Microsoft.LoadTestService/playwrightworkspaces"), "2025-07-01-preview")
    {
    }

    public BicepValue<AzureLocation> Location
    {
        get { Initialize(); return _location!; }
        set { Initialize(); _location!.Assign(value); }
    }
    private BicepValue<AzureLocation>? _location;

    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }
    private BicepValue<string>? _name;

    public BicepDictionary<string> Properties
    {
        get { Initialize(); return _properties!; }
        set { Initialize(); AssignOrReplace(ref _properties, value); }
    }
    private BicepDictionary<string>? _properties;

    protected override void DefineProvisionableProperties()
    {
        base.DefineProvisionableProperties();
        
        _location = DefineProperty<AzureLocation>(nameof(Location), ["location"], isOutput: false, isRequired: false);
        
        // Set the name using Bicep expression for unique naming
        var nameExpression = BicepFunction.Take(
            BicepFunction.Interpolate($"pw-{BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)}"),
            24);
        
        _name = DefineProperty<string>(nameof(Name), ["name"], isOutput: false, isRequired: true);
        _name.Assign(nameExpression);
        
        // Define properties
        _properties = DefineDictionaryProperty<string>(nameof(Properties), ["properties"], isOutput: false);
        _properties["regionalAffinity"] = "Enabled";
        _properties["localAuth"] = "Disabled";
    }
}
