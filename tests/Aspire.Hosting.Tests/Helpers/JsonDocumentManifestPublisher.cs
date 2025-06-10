// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tests.Helpers;

internal sealed class JsonDocumentManifestPublisher(
    ILogger<ManifestPublisher> logger,
    IOptions<PublishingOptions> options,
    DistributedApplicationExecutionContext executionContext
    ) : ManifestPublisher(logger, options, executionContext)
{
    protected override async Task PublishInternalAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new() { Indented = true });

        await WriteManifestAsync(model, writer, cancellationToken).ConfigureAwait(false);

        stream.Seek(0, SeekOrigin.Begin);
        _manifestDocument = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private JsonDocument? _manifestDocument;

    public JsonDocument ManifestDocument
    {
        get
        {
            return _manifestDocument ?? throw new InvalidOperationException("JsonDocument not available.");
        }
    }
}

internal static class JsonDocumentManifestPublisherExtensions
{
    public static JsonDocumentManifestPublisher GetManifestPublisher(this TestProgram testProgram)
    {
        var publisher = testProgram.App?.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("manifest") as JsonDocumentManifestPublisher;
        return publisher ?? throw new InvalidOperationException($"Manifest publisher was not {nameof(JsonDocumentManifestPublisher)}");
    }

    public static JsonDocumentManifestPublisher GetManifestPublisher(this IServiceProvider services)
    {
        var publisher = services.GetRequiredKeyedService<IDistributedApplicationPublisher>("manifest") as JsonDocumentManifestPublisher;
        return publisher ?? throw new InvalidOperationException($"Manifest publisher was not {nameof(JsonDocumentManifestPublisher)}");
    }
}
