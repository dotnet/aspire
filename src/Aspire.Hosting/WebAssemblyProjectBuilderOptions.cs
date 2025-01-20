// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Configurable options to supply to an instance of <see cref="ClientSideBlazorBuilder{TProject}" />.
/// </summary>
public sealed class WebAssemblyProjectBuilderOptions
{
    /// <summary>
    /// The class that should be used to serialize Aspire service discovery information and save it where the Blazor WebAssembly (client) application can use it.
    /// </summary>
    public IServiceDiscoveryInfoSerializer ServiceDiscoveryInfoSerializer { get; set; } = new WillNotSerializeAnyInfo();

    /// <summary>
    /// A null object implementation of <see cref="IServiceDiscoveryInfoSerializer" />.
    /// </summary>
    private sealed class WillNotSerializeAnyInfo : IServiceDiscoveryInfoSerializer
    {
        /// <summary>
        /// Does nothing by design.
        /// </summary>
        /// <param name="resource">The resource for which to add service discovery information.</param>
        public void SerializeServiceDiscoveryInfo(IResourceWithEndpoints resource) { }
    }

}
