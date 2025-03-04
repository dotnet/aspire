// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class KafkaContainerImageTags
{
    /// <remarks>docker.io</remarks>
    public const string Registry = "docker.io";

    /// <remarks>confluentinc/confluent-local</remarks>
    public const string Image = "confluentinc/confluent-local";

    /// <remarks>7.8.0</remarks>
    public const string Tag = "7.8.0";

    /// <remarks>kafbat/kafka-ui</remarks>
    public const string KafkaUiImage = "kafbat/kafka-ui";

    /// <remarks>v1.1.0</remarks>
    public const string KafkaUiTag = "v1.1.0";
}
