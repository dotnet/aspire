// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.RabbitMQ;

internal static class RabbitMQContainerImageTags
{
    /// <summary>docker.io</summary>
    public const string Registry = "docker.io";

    /// <summary>library/rabbitmq</summary>
    public const string Image = "library/rabbitmq";

    /// <summary>3.13</summary>
    public const string Tag = "3.13";

    /// <summary><inheritdoc cref="Tag"/>-management</summary>
    public const string TagManagement = $"{Tag}-management";
}
