// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS resource, as a marker interface for <see cref="IResource"/>'s
/// that can be deployed to an AWS.
/// </summary>
public interface IAwsResource : IResource
{
}
