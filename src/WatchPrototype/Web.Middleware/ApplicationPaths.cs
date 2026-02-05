// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    internal static class ApplicationPaths
    {
        /// <summary>
        /// The PathString all listening URLs must be registered in
        /// </summary>
        /// <value><c>/_framework/</c></value>
        public static PathString FrameworkRoot { get; } = "/_framework";

        /// <summary>
        /// An endpoint that responds with cache-clearing headers. See https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Clear-Site-Data#directives.
        /// </summary>
        /// <value><c>/_framework/clear-browser-cache</c></value>
        public static PathString ClearSiteData { get; } = FrameworkRoot + "/clear-browser-cache";

        /// <summary>
        /// Returns a JS file that handles browser refresh and showing notifications.
        /// </summary>
        /// <value><c>/_framework/aspnetcore-browser-refresh.js</c></value>
        public static PathString BrowserRefreshJS { get; } = FrameworkRoot + "/aspnetcore-browser-refresh.js";

        /// <summary>
        /// Hosts a middleware that can cache deltas sent by dotnet-watch.
        /// </summary>
        /// <value><c>/_framework/blazor-hotreload</c></value>
        public static PathString BlazorHotReloadMiddleware { get; } = FrameworkRoot + "/blazor-hotreload";

        /// <summary>
        /// Returns a JS file imported by BlazorWebAssembly as part of it's initialization. Contains
        /// scripts to apply deltas on app start.
        /// </summary>
        /// <value>/_framework/blazor-hotreload.js</value>
        public static PathString BlazorHotReloadJS { get; } = FrameworkRoot + "/blazor-hotreload.js";
    }
}
