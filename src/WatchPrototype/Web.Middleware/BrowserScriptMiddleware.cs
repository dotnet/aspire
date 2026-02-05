// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    /// <summary>
    /// Responds with the contents of WebSocketScriptInjection.js with the stub WebSocket url replaced by the
    /// one specified by the launching app.
    /// </summary>
    public sealed class BrowserScriptMiddleware
    {
        private readonly PathString _scriptPath;
        private readonly ReadOnlyMemory<byte> _scriptBytes;
        private readonly ILogger<BrowserScriptMiddleware> _logger;
        private readonly string _contentLength;

        public BrowserScriptMiddleware(RequestDelegate next, PathString scriptPath, ReadOnlyMemory<byte> scriptBytes, ILogger<BrowserScriptMiddleware> logger)
        {
            _scriptPath = scriptPath;
            _scriptBytes = scriptBytes;
            _logger = logger;
            _contentLength = _scriptBytes.Length.ToString(CultureInfo.InvariantCulture);

            logger.LogDebug("Middleware loaded. Script {scriptPath} ({size} B).", scriptPath, _contentLength);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["Content-Length"] = _contentLength;
            context.Response.Headers["Content-Type"] = "application/javascript; charset=utf-8";

            await context.Response.Body.WriteAsync(_scriptBytes, context.RequestAborted);

            _logger.LogDebug("Script injected: {scriptPath}", _scriptPath);
        }

        // for backwards compat only
        internal static ReadOnlyMemory<byte> GetBlazorHotReloadJS()
        {
            var jsFileName = "BlazorHotReload.js";
            using var stream = new MemoryStream();
            var manifestStream = typeof(BrowserScriptMiddleware).Assembly.GetManifestResourceStream(jsFileName)!;
            manifestStream.CopyTo(stream);

            return stream.ToArray();
        }

        internal static ReadOnlyMemory<byte> GetBrowserRefreshJS()
        {
            var endpoint = Environment.GetEnvironmentVariable("ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT")!;
            var serverKey = Environment.GetEnvironmentVariable("ASPNETCORE_AUTO_RELOAD_WS_KEY") ?? string.Empty;

            return GetWebSocketClientJavaScript(endpoint, serverKey);
        }

        internal static ReadOnlyMemory<byte> GetWebSocketClientJavaScript(string hostString, string serverKey)
        {
            var jsFileName = "WebSocketScriptInjection.js";
            using var reader = new StreamReader(typeof(BrowserScriptMiddleware).Assembly.GetManifestResourceStream(jsFileName)!);
            var script = reader.ReadToEnd()
                .Replace("{{hostString}}", hostString)
                .Replace("{{ServerKey}}", serverKey);

            return Encoding.UTF8.GetBytes(script);
        }
    }
}
