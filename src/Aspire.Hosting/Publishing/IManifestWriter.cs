// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.Publishing;

public interface IManifestJsonWriterFactory
{
    Utf8JsonWriter CreateJsonWriter();
}
