// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public interface IDashboardTelemetrySender
{
    public Task<HttpResponseMessage> GetTelemetryEnabledAsync();

    public Task<HttpResponseMessage> StartTelemetrySessionAsync();

    /// <summary>
    /// Posts telemetry to the server.
    /// </summary>
    /// <param name="generatedGuids">If the request will be returning properties (such as correlation or operation id) that other telemetry events need to reference *before* this request
    /// completes, a dummy guid will be generated for the number of specified properties and correlated after completion.</param>
    /// <param name="requestFunc">A function containing as inputs 1) the inner http client, and 2) a function that maps Guids to their property value, or throws if not available.</param>
    /// <returns></returns>
    public List<Guid> MakeRequest(int generatedGuids, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>> requestFunc);
}
