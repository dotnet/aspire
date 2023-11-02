// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the context for a service binding callback.
/// </summary>
/// <param name="publisherName">The name of the publisher.</param>
/// <param name="binding">The service binding annotation.</param>
public class ServiceBindingCallbackContext(string publisherName, ServiceBindingAnnotation binding)
{
    /// <summary>
    /// Gets the name of the publisher.
    /// </summary>
    public string PublisherName { get; } = publisherName;
    
    /// <summary>
    /// Gets the service binding annotation.
    /// </summary>
    public ServiceBindingAnnotation Binding { get; } = binding;
}
