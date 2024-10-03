// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class KafkaContainerImageTags
{
    /// <summary>docker.io</summary>
    public const string Registry = "docker.io";

    /// <summary>confluentinc/confluent-local</summary>
    public const string Image = "confluentinc/confluent-local";

    /// <summary>7.7.1</summary>
    public const string Tag = "7.7.1";

    /// <summary>provectuslabs/kafka-ui</summary>
    public const string KafkaUiImage = "provectuslabs/kafka-ui";

    /// <summary>v0.7.2</summary>
    public const string KafkaUiTag = "v0.7.2";
}
