// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

internal sealed class CloudFormationPhysicalResource(string name) : Resource(name);
