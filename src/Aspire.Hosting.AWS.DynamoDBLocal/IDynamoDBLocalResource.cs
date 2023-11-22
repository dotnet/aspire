// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.DynamoDBLocal;

/// <summary>
/// Represents a DynamoDB local resource. This is a dev only resources and will not be written to the project's manifest.
/// </summary>
public interface IDynamoDBLocalResource : IResourceWithEnvironment, IResourceWithBindings
{

}
