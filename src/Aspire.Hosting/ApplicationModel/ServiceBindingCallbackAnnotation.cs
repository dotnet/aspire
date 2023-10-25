// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal sealed class ServiceBindingCallbackAnnotation(string publisherName, string bindingName, Func<ServiceBindingCallbackContext, ServiceBindingAnnotation>? callback) : IResourceAnnotation
{
    public string PublisherName { get; } = publisherName;
    public string BindingName { get; } = bindingName;
    public Func<ServiceBindingCallbackContext, ServiceBindingAnnotation>? Callback { get; } = callback;
}
