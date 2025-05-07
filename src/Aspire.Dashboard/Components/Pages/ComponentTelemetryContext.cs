// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;

namespace Aspire.Dashboard.Components.Pages;

public sealed class ComponentTelemetryContextProvider
{
    private readonly DashboardTelemetryService _telemetryService;
    private string? _browserUserAgent;

    public ComponentTelemetryContextProvider(DashboardTelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    public void SetBrowserUserAgent(string? userAgent)
    {
        _browserUserAgent = userAgent;
    }

    public void Initialize(ComponentTelemetryContext context)
    {
        context.Initialize(_telemetryService, _browserUserAgent);
    }
}

public sealed class ComponentTelemetryContext : IDisposable
{
    private DashboardTelemetryService? _telemetryService;
    private OperationContext? _initializeCorrelation;
    private readonly string _componentType;
    private bool _disposed;

    public ComponentTelemetryContext(string componentType)
    {
        _componentType = componentType;
    }

    // Internal for testing
    internal Dictionary<string, AspireTelemetryProperty> Properties { get; } = [];

    private DashboardTelemetryService TelemetryService => _telemetryService ?? throw new ArgumentNullException(nameof(_telemetryService), "InitializeAsync has not been called");

    public void Initialize(DashboardTelemetryService telemetryService, string? browserUserAgent)
    {
        _telemetryService = telemetryService;

        Properties[TelemetryPropertyKeys.DashboardComponentId] = new AspireTelemetryProperty(_componentType);
        if (browserUserAgent != null)
        {
            Properties[TelemetryPropertyKeys.UserAgent] = new AspireTelemetryProperty(browserUserAgent);
        }

        _initializeCorrelation = telemetryService.PostUserTask(
            TelemetryEventKeys.ComponentInitialize,
            TelemetryResult.Success,
            properties: new Dictionary<string, AspireTelemetryProperty>
            {
                // Component properties
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(_componentType) }
            });
    }

    public bool UpdateTelemetryProperties(ReadOnlySpan<ComponentTelemetryProperty> modifiedProperties)
    {
        // Only send updated properties if they are different from the existing ones.
        var anyChange = false;

        foreach (var (name, value) in modifiedProperties)
        {
            if (value.Value is string s && string.IsNullOrEmpty(s))
            {
                continue;
            }

            if (!Properties.TryGetValue(name, out var existingValue) || !existingValue.Value.Equals(value.Value))
            {
                Properties[name] = value;
                anyChange = true;
            }
        }

        if (anyChange)
        {
            PostProperties();
        }

        return anyChange;
    }

    private void PostProperties()
    {
        TelemetryService.PostOperation(
            TelemetryEventKeys.ParametersSet,
            TelemetryResult.Success,
            properties: Properties,
            correlatedWith: _initializeCorrelation?.Properties);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            TelemetryService.PostOperation(
                TelemetryEventKeys.ComponentDispose,
                TelemetryResult.Success,
                properties: new Dictionary<string, AspireTelemetryProperty>
                {
                { TelemetryPropertyKeys.DashboardComponentId, new AspireTelemetryProperty(_componentType) }
                },
                correlatedWith: _initializeCorrelation?.Properties);

            _disposed = true;
        }
    }
}
