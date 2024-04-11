// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal sealed class DashboardLifecycleHook(IConfiguration configuration,
                                             IOptions<DashboardOptions> dashboardOptions,
                                             ILogger<DistributedApplication> distributedApplicationLogger,
                                             IDashboardEndpointProvider dashboardEndpointProvider,
                                             DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        Debug.Assert(executionContext.IsRunMode, "Dashboard resource should only be added in run mode");

        if (model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is { } dashboardResource)
        {
            ConfigureAspireDashboardResource(dashboardResource);

            // Make the dashboard first in the list so it starts as fast as possible.
            model.Resources.Remove(dashboardResource);
            model.Resources.Insert(0, dashboardResource);
        }
        else
        {
            AddDashboardResource(model);
        }

        return Task.CompletedTask;
    }

    private void AddDashboardResource(DistributedApplicationModel model)
    {
        if (dashboardOptions.Value.DashboardPath is not { } dashboardPath)
        {
            throw new DistributedApplicationException("Dashboard path empty or file does not exist.");
        }

        var fullyQualifiedDashboardPath = Path.GetFullPath(dashboardPath);
        var dashboardWorkingDirectory = Path.GetDirectoryName(fullyQualifiedDashboardPath);

        ExecutableResource? dashboardResource = default;

        if (string.Equals(".dll", Path.GetExtension(fullyQualifiedDashboardPath), StringComparison.OrdinalIgnoreCase))
        {
            // The dashboard path is a DLL, so run it with `dotnet <dll>`
            dashboardResource = new ExecutableResource(KnownResourceNames.AspireDashboard, "dotnet", dashboardWorkingDirectory ?? "");

            dashboardResource.Annotations.Add(new CommandLineArgsCallbackAnnotation(args =>
            {
                args.Add(fullyQualifiedDashboardPath);
            }));
        }
        else
        {
            // Assume the dashboard path is directly executable
            dashboardResource = new ExecutableResource(KnownResourceNames.AspireDashboard, fullyQualifiedDashboardPath, dashboardWorkingDirectory ?? "");
        }

        ConfigureAspireDashboardResource(dashboardResource);

        // Make the dashboard first in the list so it starts as fast as possible.
        model.Resources.Insert(0, dashboardResource);
    }

    private void ConfigureAspireDashboardResource(IResource dashboardResource)
    {
        // Remove endpoint annotations because we are directly configuring
        // the dashboard app (it doesn't go through the proxy!).
        var endpointAnnotations = dashboardResource.Annotations.OfType<EndpointAnnotation>().ToList();
        foreach (var endpointAnnotation in endpointAnnotations)
        {
            dashboardResource.Annotations.Remove(endpointAnnotation);
        }

        var snapshot = new CustomResourceSnapshot()
        {
            Properties = [],
            ResourceType = dashboardResource switch
            {
                ExecutableResource => KnownResourceTypes.Executable,
                ProjectResource => KnownResourceTypes.Project,
                _ => KnownResourceTypes.Container
            },
            State = configuration.GetBool("DOTNET_ASPIRE_SHOW_DASHBOARD_RESOURCES") is true ? null : KnownResourceStates.Hidden
        };

        dashboardResource.Annotations.Add(new ResourceSnapshotAnnotation(snapshot));

        dashboardResource.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
        {
            var options = dashboardOptions.Value;

            // Options should have been validated these should not be null

            Debug.Assert(options.DashboardUrl is not null, "DashboardUrl should not be null");
            Debug.Assert(options.OtlpEndpointUrl is not null, "OtlpEndpointUrl should not be null");

            var dashboardUrls = options.DashboardUrl;
            var otlpEndpointUrl = options.OtlpEndpointUrl;

            var environment = options.AspNetCoreEnvironment;
            var browserToken = options.DashboardToken;
            var otlpApiKey = options.OtlpApiKey;

            var resourceServiceUrl = await dashboardEndpointProvider.GetResourceServiceUriAsync(context.CancellationToken).ConfigureAwait(false);

            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
            context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendUrlName.EnvVarName] = dashboardUrls;
            context.EnvironmentVariables[DashboardConfigNames.ResourceServiceUrlName.EnvVarName] = resourceServiceUrl;
            context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpUrlName.EnvVarName] = otlpEndpointUrl;

            // Configure frontend browser token
            if (!string.IsNullOrEmpty(browserToken))
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendAuthModeName.EnvVarName] = "BrowserToken";
                context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendBrowserTokenName.EnvVarName] = browserToken;
            }
            else
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendAuthModeName.EnvVarName] = "Unsecured";
            }

            // Configure resource service API key
            if (string.Equals(configuration["AppHost:ResourceService:AuthMode"], nameof(ResourceServiceAuthMode.ApiKey), StringComparison.OrdinalIgnoreCase)
                && configuration["AppHost:ResourceService:ApiKey"] is { Length: > 0 } resourceServiceApiKey)
            {
                context.EnvironmentVariables[DashboardConfigNames.ResourceServiceClientAuthModeName.EnvVarName] = nameof(ResourceServiceAuthMode.ApiKey);
                context.EnvironmentVariables[DashboardConfigNames.ResourceServiceClientApiKeyName.EnvVarName] = resourceServiceApiKey;
            }
            else
            {
                context.EnvironmentVariables[DashboardConfigNames.ResourceServiceClientAuthModeName.EnvVarName] = nameof(ResourceServiceAuthMode.Unsecured);
            }

            // Configure OTLP API key
            if (!string.IsNullOrEmpty(otlpApiKey))
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpAuthModeName.EnvVarName] = "ApiKey";
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.EnvVarName] = otlpApiKey;
            }
            else
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpAuthModeName.EnvVarName] = "Unsecured";
            }

            // We need to print out the url so that dotnet watch can launch the dashboard
            // technically this is too early, but it's late ne
            if (StringUtils.TryGetUriFromDelimitedString(dashboardUrls, ";", out var firstDashboardUrl))
            {
                distributedApplicationLogger.LogInformation("Now listening on: {DashboardUrl}", firstDashboardUrl.ToString().TrimEnd('/'));
            }

            if (!string.IsNullOrEmpty(browserToken))
            {
                LoggingHelpers.WriteDashboardUrl(distributedApplicationLogger, dashboardUrls, browserToken);
            }
        }));
    }
}
