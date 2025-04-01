// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Model;

// TODO: Change this to PathBaseEnforcementMiddleware and have it return NotFound if the PathBase is not correct
//       and only add in the development environment. This will help us find issues with the dashboard when
//       PathBase is set.
internal sealed class PathBaseRedirectionMiddleware(RequestDelegate next, IOptionsMonitor<DashboardOptions> dashboardOptions, ILogger<PathBaseRedirectionMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Middleware should not be added if PathBase is not set.
        Debug.Assert(dashboardOptions.CurrentValue.PathBase is not null);

        // Redirect to path based URL if the request PathBase doesn't match configuration.
        // e.g. options.PathBase == "/dashboard/" && request.PathBase == "" && request.Path == "/login" then redirect to "/dashboard/login"
        var expectedPathBase = dashboardOptions.CurrentValue.PathBase.TrimEnd('/');
        if (context.Request.PathBase != expectedPathBase)
        {
            var requestPath = context.Request.Path;
            var requestQuery = context.Request.QueryString.ToString();
            var redirectUrl = $"{expectedPathBase}{requestPath}{requestQuery}";
            context.Response.Redirect(redirectUrl);

            logger.LogDebug("Redirecting to configured path base: {OriginalRequest} -> {RedirectingTo}", requestPath, redirectUrl);

            return Task.CompletedTask;
        }
        return next(context);
    }
}

internal static class PathBaseRedirectionMiddlewareExtensions
{
    public static IApplicationBuilder UsePathBaseRedirection(this IApplicationBuilder app)
    {
        var pathBase = app.ApplicationServices.GetService<IOptionsMonitor<DashboardOptions>>()?.CurrentValue.PathBase;

        if (pathBase is not null)
        {
            app.UseMiddleware<PathBaseRedirectionMiddleware>();
        }

        return app;
    }
}
