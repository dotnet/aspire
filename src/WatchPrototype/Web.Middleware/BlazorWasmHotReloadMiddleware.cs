// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    /// <summary>
    /// A middleware that manages receiving and sending deltas from a BlazorWebAssembly app.
    /// This assembly is shared between Visual Studio and dotnet-watch. By putting some of the complexity
    /// in here, we can avoid duplicating work in watch and VS.
    ///
    /// Mapped to <see cref="ApplicationPaths.BlazorHotReloadMiddleware"/>.
    /// </summary>
    internal sealed class BlazorWasmHotReloadMiddleware
    {
        internal sealed class Update
        {
            public int Id { get; set; }
            public Delta[] Deltas { get; set; } = default!;
        }

        internal sealed class Delta
        {
            public string ModuleId { get; set; } = default!;
            public string MetadataDelta { get; set; } = default!;
            public string ILDelta { get; set; } = default!;
            public string PdbDelta { get; set; } = default!;
            public int[] UpdatedTypes { get; set; } = default!;
        }

        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public BlazorWasmHotReloadMiddleware(RequestDelegate next, ILogger<BlazorWasmHotReloadMiddleware> logger)
        {
            logger.LogDebug("Middleware loaded");
        }

        internal List<Update> Updates { get; } = [];

        public Task InvokeAsync(HttpContext context)
        {
            // Multiple instances of the BlazorWebAssembly app could be running (multiple tabs or multiple browsers).
            // We want to avoid serialize reads and writes between then
            lock (Updates)
            {
                if (HttpMethods.IsGet(context.Request.Method))
                {
                    return OnGet(context);
                }
                else if (HttpMethods.IsPost(context.Request.Method))
                {
                    return OnPost(context);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return Task.CompletedTask;
                }
            }

            // Don't call next(). This middleware is terminal.
        }

        private async Task OnGet(HttpContext context)
        {
            if (Updates.Count == 0)
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            await JsonSerializer.SerializeAsync(context.Response.Body, Updates, s_jsonSerializerOptions);
        }

        private async Task OnPost(HttpContext context)
        {
            var update = await JsonSerializer.DeserializeAsync<Update>(context.Request.Body, s_jsonSerializerOptions);
            if (update == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // It's possible that multiple instances of the BlazorWasm are simultaneously executing and could be posting the same deltas
            // We'll use the sequence id to ensure that we're not recording duplicate entries. Replaying duplicated values would cause
            // ApplyDelta to fail.
            if (Updates is [] || Updates[^1].Id < update.Id)
            {
                Updates.Add(update);
            }
        }
    }
}
