// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class ServiceBindingCallbackContext(string publisherName, ServiceBindingAnnotation binding)
{
    public string PublisherName { get; } = publisherName;
    public ServiceBindingAnnotation Binding { get; } = binding;
}
