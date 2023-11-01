// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal sealed class ManifestJsonWriterFactory(IOptions<PublishingOptions> options) : IManifestJsonWriterFactory
{
    private readonly IOptions<PublishingOptions> _options = options;

    public Utf8JsonWriter CreateJsonWriter()
    {
        if (_options.Value.OutputPath == null)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publish manifest' argument was used."
                );
        }

        var stream = new FileStream(_options.Value.OutputPath, FileMode.Create);
        var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        return jsonWriter;
    }
}
