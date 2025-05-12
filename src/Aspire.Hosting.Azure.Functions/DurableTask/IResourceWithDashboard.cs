// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

interface IResourceWithDashboard : IResource
{
    ReferenceExpression DashboardEndpointExpression { get; }

    bool IsTaskHub => false;
}
