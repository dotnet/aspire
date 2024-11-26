// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.MongoDB;

internal static class MongoDBContainerImageTags
{
    /// <summary>docker.io</summary>
    public const string Registry = "docker.io";

    /// <summary>library/mongo</summary>
    public const string Image = "library/mongo";

    /// <summary>8.0</summary>
    public const string Tag = "8.0";

    /// <summary>docker.io</summary>
    public const string MongoExpressRegistry = "docker.io";

    /// <summary>library/mongo-express</summary>
    public const string MongoExpressImage = "library/mongo-express";

    /// <summary>1.0</summary>
    public const string MongoExpressTag = "1.0";
}
