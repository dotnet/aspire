// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal class DeferredEndpointConfigurationCallbackAnnotation(string endpointName, Action<EndpointAnnotation> callback) : IResourceAnnotation
{
    public string EndpointName { get; } = endpointName;
    public Action<EndpointAnnotation> Callback { get; } = callback;
}
