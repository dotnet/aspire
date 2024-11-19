// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Properties specific to client affine subscriptions.
/// </summary>
public class ServiceBusClientAffineProperties
{
    private readonly OptionalValue<string> _clientId = new();
    private readonly OptionalValue<bool> _isDurable = new();
    private readonly OptionalValue<bool> _isShared = new();

    /// <summary>
    /// Creates a new ServiceBusClientAffineProperties.
    /// </summary>
    public ServiceBusClientAffineProperties()
    {
    }

    /// <summary>
    /// Indicates the Client ID of the application that created the
    /// client-affine subscription.
    /// </summary>
    public OptionalValue<string> ClientId
    {
        get { return _clientId!; }
        set { _clientId!.Assign(value); }
    }

    /// <summary>
    /// For client-affine subscriptions, this value indicates whether the
    /// subscription is durable or not.
    /// </summary>
    public OptionalValue<bool> IsDurable
    {
        get { return _isDurable!; }
        set { _isDurable!.Assign(value); }
    }

    /// <summary>
    /// For client-affine subscriptions, this value indicates whether the
    /// subscription is shared or not.
    /// </summary>
    public OptionalValue<bool> IsShared
    {
        get { return _isShared!; }
        set { _isShared!.Assign(value); }
    }
}
