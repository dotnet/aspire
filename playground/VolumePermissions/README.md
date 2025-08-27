# Volume Permissions Playground

This playground demonstrates how to configure ownership and permissions for container volumes using the new `WithVolume` overloads in .NET Aspire.

## Overview

The playground shows different scenarios for volume permissions:

1. **Named volume with specific user/group ownership and permissions**
2. **Named volume with different ownership and more permissive settings**  
3. **Anonymous volume with container defaults**
4. **Read-only volume (permissions don't apply)**

## Test Container

The `testapp` container includes:
- Multiple users with specific UIDs: `appuser` (999), `datauser` (1001)
- Multiple groups with specific GIDs: `appgroup` (1001), `datagroup` (1002)
- A test script that validates volume permissions and ownership

## Key Features Demonstrated

### Volume Ownership
```csharp
.WithVolume("app-data", "/app/data", 
    userId: 999,    // appuser
    groupId: 1001   // appgroup
)
```

### Permission Modes
```csharp
directoryMode: UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
              UnixFileMode.GroupRead | UnixFileMode.GroupExecute,  // 0750
fileMode: UnixFileMode.UserRead | UnixFileMode.UserWrite |
         UnixFileMode.GroupRead                                   // 0640
```

## Usage

1. **Run the playground:**
   ```bash
   cd playground/VolumePermissions/VolumePermissions.AppHost
   dotnet run
   ```

2. **Check container logs** to see the permission test results

3. **Inspect volumes interactively:**
   ```bash
   # Get the container name from the dashboard or docker ps
   docker exec -it <container-name> /bin/bash
   
   # Check volume permissions
   ls -la /app/data /app/shared /app/readonly
   stat /app/data /app/shared /app/readonly
   ```

## What to Expect

- **app-data volume**: Owned by UID 999 (appuser), GID 1001 (appgroup), permissions 0750/0640
- **shared-data volume**: Owned by UID 999 (appuser), GID 1002 (datagroup), permissions 0775/0664  
- **default volume**: Uses container/system defaults
- **readonly-data volume**: Read-only mount (permissions not applicable)

## Provider Support

**Note:** Volume ownership and permissions are provider-specific:

- **Docker**: Best-effort support depending on volume driver and platform
- **Azure Container Apps**: Mapped to Azure Files mount options (`uid`, `gid`, `dir_mode`, `file_mode`)
- **Other providers**: May ignore these settings

This playground helps validate the behavior in your target environment and debug any permission-related issues.