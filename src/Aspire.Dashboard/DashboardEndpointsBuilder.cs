// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Api;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model.Serialization;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard;

public static class DashboardEndpointsBuilder
{
    public static void MapDashboardHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks($"/{DashboardUrls.HealthBasePath}").AllowAnonymous();
    }

    public static void MapDashboardApi(this IEndpointRouteBuilder endpoints, DashboardOptions dashboardOptions)
    {
        IEndpointConventionBuilder builder;
        if (dashboardOptions.Frontend.AuthMode == FrontendAuthMode.BrowserToken)
        {
            builder = endpoints.MapPost("/api/validatetoken", async (string token, HttpContext httpContext, IOptionsMonitor<DashboardOptions> dashboardOptions) =>
            {
                return await ValidateTokenMiddleware.TryAuthenticateAsync(token, httpContext, dashboardOptions).ConfigureAwait(false);
            });

#if DEBUG
            // Available in local debug for testing.
            endpoints.MapGet("/api/signout", async (HttpContext httpContext) =>
            {
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    httpContext,
                    CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
                httpContext.Response.Redirect("/");
            }).SkipStatusCodePages();
#endif
        }
        else
        {
            builder = endpoints.MapPostNotFound("/api/validatetoken");
        }
        builder.SkipStatusCodePages();

        if (dashboardOptions.Frontend.AuthMode == FrontendAuthMode.OpenIdConnect)
        {
            endpoints.MapPost("/authentication/logout", () => TypedResults.SignOut(authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme])).SkipStatusCodePages();
        }

        endpoints.MapGet("/api/set-language", async (string? language, string? redirectUrl, [FromHeader(Name = "Accept-Language")] string? acceptLanguage, HttpContext httpContext) =>
        {
            if (string.IsNullOrEmpty(redirectUrl))
            {
                return Results.BadRequest();
            }

            // The passed in language should be one of the localized cultures.
            var newLanguage = GlobalizationHelpers.OrderedLocalizedCultures.SingleOrDefault(c => string.Equals(c.Name, language, StringComparisons.CultureName));
            if (newLanguage == null)
            {
                return Results.BadRequest();
            }

            if (!GlobalizationHelpers.ExpandedLocalizedCultures.TryGetValue(newLanguage.Name, out var availableCultures))
            {
                return Results.BadRequest();
            }

            // The passed in language is one of the supported localized cultures. e.g. en, fr, de, etc.
            // However, if the browser specifies a culture via accept-language header that is compatible with the language, then we want to use that.
            // For example, the new language is "en" and accept-language is "en-GB", then we want to use "en-GB".
            RequestCulture? requestCulture = null;
            if (acceptLanguage != null)
            {
                requestCulture = await GlobalizationHelpers.ResolveSetCultureToAcceptedCultureAsync(acceptLanguage, availableCultures).ConfigureAwait(false);
            }
            requestCulture ??= new RequestCulture(newLanguage.Name, newLanguage.Name);

            httpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }); // consistent with theme cookie expiry

            return Results.LocalRedirect(redirectUrl);
        }).SkipStatusCodePages();
    }

    public static void MapDashboardMcp(this IEndpointRouteBuilder endpoints, DashboardOptions dashboardOptions)
    {
        IEndpointConventionBuilder builder;
        if (!dashboardOptions.Mcp.Disabled.GetValueOrDefault())
        {
            builder = endpoints.MapMcp("/mcp").RequireAuthorization(McpApiKeyAuthenticationHandler.PolicyName);
        }
        else
        {
            builder = endpoints.MapPostNotFound("/mcp");
        }
        builder.SkipStatusCodePages();
    }

    public static void MapTelemetryApi(this IEndpointRouteBuilder endpoints, DashboardOptions dashboardOptions)
    {
        if (dashboardOptions.Mcp.Disabled.GetValueOrDefault())
        {
            return;
        }

        var group = endpoints.MapGroup("/api/telemetry")
            .RequireAuthorization(TelemetryApiAuthenticationHandler.PolicyName)
            .SkipStatusCodePages()
            .WithTags("Telemetry");

        // GET /api/telemetry/traces - List traces in OTLP JSON format (with optional streaming via ?follow=true)
        group.MapGet("/traces", async (
            TelemetryApiService service,
            HttpContext httpContext,
            [FromQuery] string? resource,
            [FromQuery] bool? hasError,
            [FromQuery] int? limit,
            [FromQuery] bool? follow,
            CancellationToken cancellationToken) =>
        {
            if (follow == true)
            {
                // Stream NDJSON
                httpContext.Response.ContentType = "application/x-ndjson";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers["X-Accel-Buffering"] = "no";

                await foreach (var json in service.FollowTracesAsync(resource, hasError, limit, cancellationToken).ConfigureAwait(false))
                {
                    await httpContext.Response.WriteAsync(json, cancellationToken).ConfigureAwait(false);
                    await httpContext.Response.WriteAsync("\n", cancellationToken).ConfigureAwait(false);
                    await httpContext.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var response = service.GetTraces(resource, hasError, limit);
                return Results.Json(response, OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
            }

            return Results.Empty;
        });

        // GET /api/telemetry/traces/{traceId} - Get single trace in OTLP JSON format
        group.MapGet("/traces/{traceId}", Results<ContentHttpResult, NotFound<ProblemDetails>> (
            TelemetryApiService service,
            string traceId) =>
        {
            var json = service.GetTraceById(traceId);
            if (json is null)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Trace not found",
                    Detail = $"No trace with ID '{traceId}' was found.",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return TypedResults.Content(json, "application/json");
        });

        // GET /api/telemetry/traces/{traceId}/logs - Get logs for a trace in OTLP JSON format
        group.MapGet("/traces/{traceId}/logs", (
            TelemetryApiService service,
            string traceId) =>
        {
            var response = service.GetTraceLogs(traceId);
            return Results.Json(response, OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
        });

        // GET /api/telemetry/logs - List logs in OTLP JSON format (with optional streaming via ?follow=true)
        group.MapGet("/logs", async (
            TelemetryApiService service,
            HttpContext httpContext,
            [FromQuery] string? resource,
            [FromQuery] string? traceId,
            [FromQuery] string? severity,
            [FromQuery] int? limit,
            [FromQuery] bool? follow,
            CancellationToken cancellationToken) =>
        {
            if (follow == true)
            {
                // Stream NDJSON
                httpContext.Response.ContentType = "application/x-ndjson";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers["X-Accel-Buffering"] = "no";

                await foreach (var json in service.FollowLogsAsync(resource, traceId, severity, limit, cancellationToken).ConfigureAwait(false))
                {
                    await httpContext.Response.WriteAsync(json, cancellationToken).ConfigureAwait(false);
                    await httpContext.Response.WriteAsync("\n", cancellationToken).ConfigureAwait(false);
                    await httpContext.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var response = service.GetLogs(resource, traceId, severity, limit);
                return Results.Json(response, OtlpJsonSerializerContext.Default.TelemetryApiResponseOtlpTelemetryDataJson);
            }

            return Results.Empty;
        });

        // GET /api/telemetry/logs/{logId} - Get single log entry in OTLP JSON format
        group.MapGet("/logs/{logId:long}", Results<ContentHttpResult, NotFound<ProblemDetails>> (
            TelemetryApiService service,
            long logId) =>
        {
            var json = service.GetLogById(logId);
            if (json is null)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Log entry not found",
                    Detail = $"No log entry with ID '{logId}' was found.",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return TypedResults.Content(json, "application/json");
        });
    }
}
