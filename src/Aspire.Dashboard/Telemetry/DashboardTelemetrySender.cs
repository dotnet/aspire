// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Aspire.Dashboard.Telemetry;

public class DashboardTelemetrySender : IDashboardTelemetrySender
{
    private readonly HttpClient _httpClient;

    private readonly Channel<(List<Guid>, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>>)> _channel;
    private readonly ConcurrentDictionary<Guid, object> _responsePropertyMap = [];

    public DashboardTelemetrySender(HttpClient client, ILogger<DashboardTelemetryService> logger)
    {
        _channel = Channel.CreateBounded<(List<Guid>, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>>)>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        _httpClient = client;

        _ = Task.Run(async () =>
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var operation))
                {
                    var (propertyIds, requestFunc) = operation;
                    try
                    {
                        var result = await requestFunc(client, GetResponseProperty).ConfigureAwait(false);

                        // Each property id corresponds to a value that hasn't yet been received from the telemetry server.
                        // We need to associate with values received so that they can be retrieved by future requests that may be referencing these values (using these guids)
                        foreach (var (propertyId, value) in propertyIds.Zip(result))
                        {
                            _responsePropertyMap[propertyId] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send telemetry request.");
                    }
                }
            }
        });
    }

    private object GetResponseProperty(Guid propertyId)
    {
        if (!_responsePropertyMap.TryGetValue(propertyId, out var value))
        {
            throw new InvalidOperationException($"Response property not found. Id: {propertyId}");
        }

        return value;
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

        var propertyIds = new List<Guid>();
        for (var i = 0; i < generatedGuids; i++)
        {
            propertyIds.Add(Guid.NewGuid());
        }

        _channel.Writer.TryWrite((propertyIds, requestFunc));

        return propertyIds;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _channel.Writer.Complete();
    }
}
