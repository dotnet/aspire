// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class DaprDataPlane : IDashboardExtension
{
    private readonly ILogger<DaprDataPlane> _logger;
    private readonly StateStore _stateStore;

    public DaprDataPlane(ILogger<DaprDataPlane> logger, StateStore stateStore)
    {
        _logger = logger;
        _stateStore = stateStore;
    }

    public void ConfigureRoutes(IEndpointRouteBuilder builder)
    {
        var v1Group =
            builder
                .MapGroup("dapr/v1.0")
                .WithName("Dapr v1.0");

        var stateStoreGroup =
            v1Group
                .MapGroup("/statestore")
                .WithName("StateStore");

        stateStoreGroup
            .MapGet(
                "/keys",
                () =>
                {
                    return _stateStore.GetKeysAsync();
                })
            .WithName("GetStateStoreKeys")
            .WithOpenApi();

        stateStoreGroup
            .MapGet(
                "/keys/{key}",
                async (string key) =>
                {
                    var value = await _stateStore.GetKeyAsync(key).ConfigureAwait(false);

                    return value is not null
                            ? Results.Content(value, "application/json")
                            : Results.NotFound(key);
                })
            .WithName("GetStateStoreKey")
            .WithOpenApi();

        stateStoreGroup
            .MapDelete(
                "/keys/{key}",
                async (string key) =>
                {
                    await _stateStore.DeleteAsync(key).ConfigureAwait(false);

                    return Results.NoContent();
                })
            .WithName("DeleteStateStoreKey")
            .WithOpenApi();

        stateStoreGroup
            .MapPut(
                "/keys/{key}",
                async (string key, [FromBody] string content) =>
                {
                    await _stateStore.SetAsync(key, content).ConfigureAwait(false);

                    return Results.Accepted();
                })
            .WithName("SetStateStoreKey")
            .WithOpenApi();
    }
}
