// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Kubernetes.Helm;

/// <summary>
/// Represents configuration options specific to the Helm publisher for deploying applications
/// to Kubernetes using Helm charts. Inherits common publishing options from <see cref="PublishingOptions"/>.
/// </summary>
public sealed class HelmPublisherOptions : PublishingOptions;
