// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Aspire.Dashboard.Utils;

internal static class CookieAuthUtils
{
    public static void AdjustForPathBase(this CookieAuthenticationOptions options)
    {
        options.Events.OnRedirectToLogin = GetPathBaseAwareRedirectEventHandler(o => o.LoginPath);
        options.Events.OnRedirectToLogout = GetPathBaseAwareRedirectEventHandler(o => o.LogoutPath);
        options.Events.OnRedirectToAccessDenied = GetPathBaseAwareRedirectEventHandler(o => o.AccessDeniedPath);
    }

    public static Func<RedirectContext<CookieAuthenticationOptions>, Task> GetPathBaseAwareRedirectEventHandler(
        Func<CookieAuthenticationOptions, PathString> getPath) => context =>
        {
            // Check if the request path starts with the specified path base and if so, update the redirect path approriately
            var path = getPath(context.Options);
            var redirectUrl = context.Request.PathBase.HasValue ? context.Request.PathBase + path : path;
            context.HttpContext.Response.Redirect(redirectUrl);
            return Task.CompletedTask;
        };
}
