// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Aspire.Dashboard;

public static class DashboardEndpointsBuilder
{
    public static void MapDashboardBlazor(this IEndpointRouteBuilder endpoints)
    {
        var options = new StaticFileOptions
        {
            FileProvider = new ManifestEmbeddedFileProvider(typeof(DashboardEndpointsBuilder).Assembly),
            OnPrepareResponse = SetCacheHeaders
        };

        var app = endpoints.CreateApplicationBuilder();
        app.Use(next => context =>
        {
            // Set endpoint to null so the static files middleware will handle the request.
            context.SetEndpoint(null);

            return next(context);
        });
        app.UseStaticFiles(options);

        endpoints.MapGet("/_aspire/blazor.web.js", app.Build());

        static void SetCacheHeaders(StaticFileResponseContext ctx)
        {
            // By setting "Cache-Control: no-cache", we're allowing the browser to store
            // a cached copy of the response, but telling it that it must check with the
            // server for modifications (based on Etag) before using that cached copy.
            // Longer term, we should generate URLs based on content hashes (at least
            // for published apps) so that the browser doesn't need to make any requests
            // for unchanged files.
            var headers = ctx.Context.Response.GetTypedHeaders();
            if (headers.CacheControl == null)
            {
                headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
            }
        }
    }

    public static void MapDashboardHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks($"/{DashboardUrls.HealthBasePath}").AllowAnonymous();
    }

    public static void MapDashboardApi(this IEndpointRouteBuilder endpoints, DashboardOptions dashboardOptions)
    {
        if (dashboardOptions.Frontend.AuthMode == FrontendAuthMode.BrowserToken)
        {
            endpoints.MapPost("/api/validatetoken", async (string token, HttpContext httpContext, IOptionsMonitor<DashboardOptions> dashboardOptions) =>
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
            });
#endif
        }
        else if (dashboardOptions.Frontend.AuthMode == FrontendAuthMode.OpenIdConnect)
        {
            endpoints.MapPost("/authentication/logout", () => TypedResults.SignOut(authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));
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
        });
    }
}
