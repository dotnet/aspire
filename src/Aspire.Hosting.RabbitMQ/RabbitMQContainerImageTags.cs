// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.RabbitMQ;

internal static class RabbitMQContainerImageTags
{
    public const string Registry = "docker.io";
    public const string Image = "library/rabbitmq";
    public const string Tag = "3";
    public const string TagManagement = $"{Tag}-management";
}
