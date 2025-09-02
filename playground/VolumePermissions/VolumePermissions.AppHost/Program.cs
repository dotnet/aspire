// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


var builder = DistributedApplication.CreateBuilder(args);

// Demonstrate volume ownership and permissions configuration

// Example 1: Named volume with specific ownership and permissions
// - Owner: appuser (UID 999)  
// - Group: appgroup (GID 1001)
// - Directory permissions: 0750 (rwx r-x ---)
// - File permissions: 0640 (rw- r-- ---)
var container1 = builder.AddContainer("permissions-demo-1", "permissions-test")
    .WithDockerfile("testapp")
    .WithVolume("app-data", "/app/data", isReadOnly: false,
        userId: 999, groupId: 1001,
        directoryMode: UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                      UnixFileMode.GroupRead | UnixFileMode.GroupExecute,
        fileMode: UnixFileMode.UserRead | UnixFileMode.UserWrite |
                 UnixFileMode.GroupRead);

// Example 2: Named volume with different ownership and more permissive settings
// - Owner: appuser (UID 999)
// - Group: datagroup (GID 1002)  
// - Directory permissions: 0775 (rwx rwx r-x)
// - File permissions: 0664 (rw- rw- r--)
var container2 = builder.AddContainer("permissions-demo-2", "permissions-test")
    .WithDockerfile("testapp")
    .WithVolume("shared-data", "/app/shared", isReadOnly: false,
        userId: 999, groupId: 1002,
        directoryMode: UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                      UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                      UnixFileMode.OtherRead | UnixFileMode.OtherExecute,
        fileMode: UnixFileMode.UserRead | UnixFileMode.UserWrite |
                 UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                 UnixFileMode.OtherRead);

// Example 3: Anonymous volume with default permissions for the container user
// No specific ownership/permissions set - uses container defaults
var container3 = builder.AddContainer("permissions-demo-3", "permissions-test")
    .WithDockerfile("testapp") 
    .WithVolume("/app/default");

// Example 4: Read-only volume (permissions don't apply to read-only mounts)
var container4 = builder.AddContainer("permissions-demo-4", "permissions-test")
    .WithDockerfile("testapp")
    .WithVolume("readonly-data", "/app/readonly", isReadOnly: true);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();