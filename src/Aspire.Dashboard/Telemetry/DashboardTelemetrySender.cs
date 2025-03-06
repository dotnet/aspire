// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;

namespace Aspire.Dashboard.Telemetry;

public class DashboardTelemetrySender : IDashboardTelemetrySender
{
    private readonly HttpClient _httpClient;

    private readonly Channel<(List<Guid>, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>>)> _channel = Channel.CreateUnbounded<(List<Guid>, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>>)>();
    private readonly ConcurrentDictionary<Guid, object> _responsePropertyMap = [];

    public DashboardTelemetrySender(HttpClient client, ILogger<IDashboardTelemetryService> logger)
    {
        _httpClient = client;
        _ = Task.Run(async () =>
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var operation))
                {
                    var (guids, requestFunc) = operation;
                    try
                    {
                        var result = await requestFunc(client, GetResponseProperty).ConfigureAwait(false);
                        foreach (var (guid, value) in guids.Zip(result))
                        {
                            _responsePropertyMap[guid] = value;
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException or ArgumentException)
                    {
                        logger.LogWarning("Failed to make telemetry request: {ExceptionMessage}", ex.Message);
                    }

                    continue;

                    object GetResponseProperty(Guid guid)
                    {
                        if (!_responsePropertyMap.TryGetValue(guid, out var value))
                        {
                            throw new ArgumentException("Response property not found, maybe a dependent telemetry request failed?", nameof(guid));
                        }

                        return value;
                    }
                }
            }
        });
    }

    public Task<HttpResponseMessage> GetTelemetryEnabledAsync()
    {
        return _httpClient.GetAsync(TelemetryEndpoints.TelemetryEnabled);
    }

    public Task<HttpResponseMessage> StartTelemetrySessionAsync()
    {
        return _httpClient.PostAsync(TelemetryEndpoints.TelemetryStart, content: null);
    }

    public List<Guid> MakeRequest(int generatedGuids, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>> requestFunc)
    {
        Debug.Assert(generatedGuids >= 0, "guidsNeeded must be >= 0");

        var guids = new List<Guid>();
        for (var i = 0; i < generatedGuids; i++)
        {
            guids.Add(Guid.NewGuid());
        }

        _channel.Writer.TryWrite((guids, requestFunc));

        return guids;
    }
}
