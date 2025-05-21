// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal interface IResourceWithDirectConnectionString : IResource
{
    ReferenceExpression DirectConnectionStringExpression { get; }
}

internal sealed record MongoDbReplicaSetAnnotation(string ReplicaSetName) : IResourceAnnotation
{
    internal const string QueryName = "replicaSet";
}
