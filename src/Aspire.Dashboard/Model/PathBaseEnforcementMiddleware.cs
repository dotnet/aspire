// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Model;

internal sealed class PathBaseEnforcementMiddleware(RequestDelegate next, IOptionsMonitor<DashboardOptions> dashboardOptions, ILogger<PathBaseEnforcementMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Middleware should not be added if PathBase is not set.
        Debug.Assert(dashboardOptions.CurrentValue.PathBase is not null);

        // Return a 404 if the request PathBase doesn't match configuration, except for the login page.
        // Redirect to path based URL if the request PathBase doesn't match configuration.
        // e.g. options.PathBase == "/dashboard/" && request.PathBase == "" && request.Path == "/login" then redirect to "/dashboard/login"
        var expectedPathBase = dashboardOptions.CurrentValue.PathBase.TrimEnd('/');
        if (context.Request.PathBase != expectedPathBase)
        {
            var requestPath = context.Request.Path;

            if (requestPath.StartsWithSegments("/login"))
            {
                var requestQuery = context.Request.QueryString.ToString();
                var redirectUrl = $"{expectedPathBase}{requestPath}{requestQuery}";
                context.Response.Redirect(redirectUrl);

                logger.LogDebug("Redirecting to configured path base: {OriginalRequest} -> {RedirectingTo}", requestPath, redirectUrl);
                return Task.CompletedTask;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }
        return next(context);
    }
}

internal static class PathBaseEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UsePathBaseEnforcement(this IApplicationBuilder app)
    {
        var pathBase = app.ApplicationServices.GetService<IOptionsMonitor<DashboardOptions>>()?.CurrentValue.PathBase;

        if (pathBase is not null)
        {
            app.UseMiddleware<PathBaseEnforcementMiddleware>();
        }

        return app;
    }
}
