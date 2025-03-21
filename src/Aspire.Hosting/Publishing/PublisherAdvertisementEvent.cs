// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.Publishing;

internal sealed class PublisherAdvertisementEvent : IDistributedApplicationEvent
{
    public void AddAdvertisement(string name)
    {
        var advertisement = new PublisherAdvertisement(name);
        _advertisements.Add(advertisement);
    }

    private readonly List<PublisherAdvertisement> _advertisements = [];

    public IEnumerable<PublisherAdvertisement> Advertisements => _advertisements;
}

internal sealed class PublisherAdvertisement(string name)
{
    public string Name { get; } = name;
}