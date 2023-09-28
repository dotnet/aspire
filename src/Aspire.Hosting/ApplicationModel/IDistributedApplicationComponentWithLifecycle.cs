// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public interface IDistributedApplicationComponentWithLifecycle : IDistributedApplicationComponent
{
    void OnStarting();
    void OnStarted();
    void OnStopping();
    void OnStopped();
}
