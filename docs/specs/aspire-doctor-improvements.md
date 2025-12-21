# Aspire Doctor Command Improvements

Tracking ideas for new checks to add to `aspire doctor` based on customer issues and common pain points.

## Check Categories

Checks are organized into three categories:

1. **Machine-wide Setup** - Global environment checks (no project needed)
2. **Project-specific** - Checks that analyze AppHost project/directory (AppHost not running)
3. **Runtime-specific** - Checks that need data from a running AppHost

---

## Category 1: Machine-wide Setup Checks

These checks validate the developer's machine environment. They can run anywhere without needing an Aspire project and don't depend on what resources are configured in any AppHost.

### 1.1 Deprecated Aspire Workload Detection
**Priority:** High
**Source:** Internal knowledge, [#11202](https://github.com/dotnet/aspire/issues/11202)

The `aspire` workload is deprecated and should be uninstalled. Users with this workload installed may encounter conflicts or confusion.

**Check logic:**
- Run `dotnet workload list` and parse output
- If `aspire` workload is found, return Warning

**Fix suggestion:**
```
dotnet workload uninstall aspire
```

---

### 1.2 IDE Certificate Expiry Detection
**Priority:** Medium
**Source:** [#6207](https://github.com/dotnet/aspire/issues/6207), [#5548](https://github.com/dotnet/aspire/issues/5548)

IDE-generated certificates expire after 7 days. Users leaving VS open for extended periods encounter:
```
tls: failed to verify certificate: x509: certificate has expired or is not yet valid
```

**Check logic:**
- Locate IDE session certificates (VS/Rider/VS Code)
- Check expiration dates
- Warn if expired or expiring soon

**Fix suggestion:**
```
Restart your IDE to regenerate the session certificate
```

---

### 1.3 Container Runtime Configuration (Enhanced)
**Priority:** Medium
**Source:** [#10199](https://github.com/dotnet/aspire/issues/10199), [#6635](https://github.com/dotnet/aspire/issues/6635)

Current check validates Docker/Podman, but could be enhanced:

**Additional checks:**
- Rancher Desktop with `nerdctl` CLI (warn: use `docker` CLI mode instead)
- Docker Desktop WSL integration enabled (for WSL users)
- Podman machine running (macOS/Windows)
- containerd image store enabled for multi-arch builds ([#12595](https://github.com/dotnet/aspire/issues/12595))

---

### 1.4 Docker Windows Container Mode Detection
**Priority:** Medium
**Source:** [#11389](https://github.com/dotnet/aspire/issues/11389)

Docker running in Windows container mode causes confusing errors when pulling Linux images.

**Error pattern:**
```
no matching manifest for windows/amd64 in the manifest list entries
```

**Check logic:**
- Run `docker version --format '{{.Server.Os}}'`
- If result is "windows", warn that Linux containers are required

**Fix suggestion:**
```
Switch Docker Desktop to Linux containers mode (right-click Docker tray icon)
```

---

### 1.5 Docker Proxy Configuration
**Priority:** Low
**Source:** [#11413](https://github.com/dotnet/aspire/issues/11413)

Corporate proxy settings can prevent Docker from pulling images.

**Check logic:**
- Check for HTTP_PROXY/HTTPS_PROXY environment variables
- Verify Docker daemon proxy configuration if set
- Test ability to pull a small image

---

### 1.6 Docker Desktop Resource Saver Mode
**Priority:** Medium
**Source:** [Discussion #9739](https://github.com/dotnet/aspire/discussions/9739)

Docker Desktop Resource Saver mode can cause "container runtime appears unhealthy" warnings.

**Warning pattern:**
```
Container runtime 'docker' was found but appears to be unhealthy.
If Resource Saver mode is enabled, containers may not run.
```

**Check logic:**
- Check Docker Desktop settings for Resource Saver mode
- Warn if enabled and containers fail to start

---

### 1.7 NuGet Connectivity
**Priority:** Medium
**Source:** [Discussion #12518](https://github.com/dotnet/aspire/discussions/12518), [#13433](https://github.com/dotnet/aspire/issues/13433)

Aspire projects require NuGet package restore. Corporate firewalls or offline environments cause failures.

**Check logic:**
- Test connectivity to nuget.org
- Check for configured private feeds
- Warn about potential restore failures

---

### 1.8 Temp Directory Permissions
**Priority:** Low
**Source:** [Discussion #1671](https://github.com/dotnet/aspire/discussions/1671)

DCP creates temp files for stdout/stderr capture. Permission or path issues cause "run session could not be started" errors.

**Error pattern:**
```
failed to create temporary file for capturing standard output data
```

**Check logic:**
- Verify TEMP directory exists and is writable
- Check for stale temp files from previous runs

---

### 1.9 Docker Snap Installation (Linux)
**Priority:** Medium
**Source:** [#11027](https://github.com/dotnet/aspire/issues/11027)

Docker installed via Snap on Linux causes "docker command appears to be aliased" errors because snap symlinks point to `/usr/bin/snap`.

**Error pattern:**
```
docker command appears to be aliased to a different container runtime
```

**Check logic:**
- On Linux, check if docker is installed via snap (`/snap/bin/docker` symlinks to `/usr/bin/snap`)
- Warn that `ASPIRE_CONTAINER_RUNTIME=docker` may be needed

**Fix suggestion:**
```
Set environment variable: ASPIRE_CONTAINER_RUNTIME=docker
Or install Docker via apt/dnf instead of snap
```

---

### 1.10 localhost DNS Resolution
**Priority:** Low
**Source:** [#10754](https://github.com/dotnet/aspire/issues/10754)

Containers may be accessible on `127.0.0.1` but not `localhost` if IPv6 is misconfigured or hosts file has issues.

**Check logic:**
- Verify `localhost` resolves correctly
- Check hosts file configuration
- Warn about potential connection issues if mismatch detected

---

### 1.11 Windows Dev Drive Configuration
**Priority:** Low
**Source:** [#10268](https://github.com/dotnet/aspire/issues/10268)

NuGet packages on a Dev Drive mounted as a folder (junction) causes DCP failures.

**Error pattern:**
```
directory junctions are not supported, use directory symbolic link instead
```

**Check logic:**
- On Windows, check if NUGET_PACKAGES env var points to a junction
- Warn about Dev Drive mount configuration

**Fix suggestion:**
```
Use a symbolic link instead of mounting Dev Drive as a folder
Or set NUGET_PACKAGES to a non-junction path
```

---

### 1.12 Corporate Network/VPN Detection
**Priority:** Low
**Source:** [#9376](https://github.com/dotnet/aspire/issues/9376)

Corporate security software (Cisco Umbrella, ZScaler, etc.) can interfere with DNS resolution and container networking.

**Check logic:**
- Detect known corporate security software processes
- Warn about potential DNS/networking interference if Aspire fails to start
- Suggest checking with IT if issues persist

---

### 1.13 Docker Desktop Slow Response
**Priority:** Medium
**Source:** [#7802](https://github.com/dotnet/aspire/issues/7802)

Certain Docker Desktop versions cause `docker info` to take 20+ seconds, leading to timeout errors.

**Error pattern:**
```
Container runtime 'docker' could not be found
Polly.Timeout.TimeoutRejectedException
```

**Check logic:**
- Time `docker container ls -n 1` command
- If slow (>5 seconds), warn about Docker Desktop performance issues
- Suggest downgrading Docker Desktop or restarting

---

### 1.14 Dev Container Configuration
**Priority:** Low
**Source:** [#6830](https://github.com/dotnet/aspire/issues/6830)

Aspire doesn't support `docker-outside-of-docker` in dev containers. Must use `docker-in-docker`.

**Check logic:**
- Detect if running in a dev container (check for `.devcontainer` markers)
- Check devcontainer.json for docker-outside-of-docker feature
- Warn that docker-in-docker is required

---

### 1.15 Conflicting Environment Variables
**Priority:** High
**Source:** [#5389](https://github.com/dotnet/aspire/issues/5389), [#5475](https://github.com/dotnet/aspire/issues/5475)

Certain environment variable names conflict with Aspire/DCP internals or cause IDE timeouts.

**Error patterns:**
```
An element with the same key but a different value already exists. Key: 'name'
timeout of 120 seconds exceeded waiting for the IDE to start a run session
```

**Check logic:**
- Check for reserved env var names: `name`, `shell` (lowercase) on Linux/WSL
- Check for extremely long env vars (>10k characters) that can cause timeouts
- Warn about Nix shell / direnv configurations that set these

**Fix suggestion:**
```
Unset conflicting environment variables before running Aspire:
unset name shell
```

---

### 1.16 Proxy Environment Variables
**Priority:** Medium
**Source:** [#1012](https://github.com/dotnet/aspire/issues/1012), [#3355](https://github.com/dotnet/aspire/issues/3355)

HTTP_PROXY/HTTPS_PROXY settings can break Aspire's internal communications to localhost.

**Error pattern:**
```
403 Forbidden
An attempt was made to access a socket in a way forbidden by its access permissions
```

**Check logic:**
- Check if HTTP_PROXY/HTTPS_PROXY are set
- Check if NO_PROXY includes `localhost,127.0.0.1,::1`
- Warn if localhost/loopback not excluded from proxy

**Fix suggestion:**
```
Add to NO_PROXY: localhost,127.0.0.1,::1
```

---

### 1.17 IPv6 Localhost Availability
**Priority:** Medium
**Source:** [#3355](https://github.com/dotnet/aspire/issues/3355)

Corporate VPNs or network configurations that block IPv6 localhost (::1) cause Aspire startup failures.

**Error pattern:**
```
System.Net.Sockets.SocketException (10013): An attempt was made to access a socket in a way forbidden
```

**Check logic:**
- Test if `::1` (IPv6 localhost) is accessible
- Check if corporate VPN software is running
- Warn if IPv6 localhost is blocked

**Fix suggestion:**
```
Contact IT to allow IPv6 localhost access, or check VPN configuration
```

---

### 1.18 OTEL Environment Variable Conflicts
**Priority:** Low
**Source:** [#6939](https://github.com/dotnet/aspire/issues/6939)

User-configured OTEL exporters (e.g., Seq) may conflict with Aspire's default `OTEL_EXPORTER_OTLP_PROTOCOL=grpc` setting.

**Check logic:**
- Detect if custom OTEL environment variables are set
- Warn about potential conflicts with Aspire's OTEL configuration
- Suggest checking telemetry pipeline if logs not appearing

---

### 1.19 Group Policy Blocking Aspire Tools
**Priority:** High
**Source:** [#6855](https://github.com/dotnet/aspire/issues/6855)

Corporate group policies can block execution of `Aspire.RuntimeIdentifier.Tool`, preventing Aspire from starting.

**Error pattern:**
```
Failed to run Aspire.RuntimeIdentifier.Tool. Exit code: 1.
Output: This program is blocked by group policy.
```

**Check logic:**
- Try to execute `Aspire.RuntimeIdentifier.Tool` and check exit code
- Detect group policy blocking message
- Suggest workaround or running as Administrator

**Fix suggestion:**
```
Add to AppHost .csproj:
<SkipAddAspireDefaultReferences>true</SkipAddAspireDefaultReferences>
And manually add platform-specific package references
Or contact IT to whitelist Aspire tools
```

---

### 1.20 Corporate Certificate Policy
**Priority:** High
**Source:** [#7443](https://github.com/dotnet/aspire/issues/7443), [#9158](https://github.com/dotnet/aspire/issues/9158)

Corporate policies blocking self-signed certificates prevent Aspire's internal TLS communication.

**Error pattern:**
```
The SSL connection could not be established
The remote certificate was rejected by the provided RemoteCertificateValidationCallback
```

**Check logic:**
- Check if dev-certs are trusted
- Test if self-signed certificates can be used locally
- Detect corporate certificate interception (e.g., Zscaler, corporate root CA)

**Fix suggestion:**
```
Try: ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
Or work with IT to allow Aspire's self-signed certificates
```

---

### 1.21 HTTP/2 Proxy Interference
**Priority:** Medium
**Source:** [#1818](https://github.com/dotnet/aspire/issues/1818)

Corporate proxies that don't support HTTP/2 break OTEL telemetry connections.

**Error pattern:**
```
Requesting HTTP version 2.0 with version policy RequestVersionOrHigher while unable to establish HTTP/2 connection
```

**Check logic:**
- Detect if HTTP_PROXY is set
- Test HTTP/2 connectivity to localhost
- Warn about proxy HTTP/2 compatibility issues

---

### 1.22 CI/CD Environment Dev Certs
**Priority:** Medium
**Source:** [#9061](https://github.com/dotnet/aspire/issues/9061)

GitHub Actions and other CI environments don't have dev-certs trusted by default.

**Check logic:**
- Detect CI environment (GITHUB_ACTIONS, TF_BUILD, CI env vars)
- Check if dev-certs are trusted
- Warn about SSL errors in CI

**Fix suggestion:**
```
Add to CI pipeline:
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

---

## Category 2: Project-specific Checks

These checks analyze an AppHost project or solution directory by inspecting project files (.csproj, launchSettings.json, etc.). The AppHost doesn't need to be running and these checks do NOT parse AppHost.cs code.

### 2.1 Dashboard Port Availability
**Priority:** Medium
**Source:** [#3044](https://github.com/dotnet/aspire/issues/3044), [#5112](https://github.com/dotnet/aspire/issues/5112)

Dashboard fails to start when its default ports are in use.

**Check logic:**
- Check if ports 18888, 18889 (default dashboard ports) are available
- Check launchSettings.json for custom dashboard ports

---

### 2.2 Aspire SDK Version Consistency
**Priority:** Medium
**Source:** [#12274](https://github.com/dotnet/aspire/issues/12274)

Mismatched or missing SDK versions cause confusing errors (e.g., VS asking for "Desktop Experience" workload).

**Check logic:**
- Parse AppHost .csproj for `Aspire.AppHost.Sdk` version
- Verify SDK package exists in NuGet cache
- Check for version mismatches across solution

---

### 2.3 launchSettings.json Validation
**Priority:** Medium
**Source:** [#7975](https://github.com/dotnet/aspire/issues/7975)

Invalid JSON in launchSettings.json (e.g., trailing commas) causes silent failures.

**Check logic:**
- Parse launchSettings.json with strict JSON validation
- Report syntax errors with line numbers

---

### 2.4 Central Package Management Detection
**Priority:** Low
**Source:** [#13241](https://github.com/dotnet/aspire/issues/13241)

`aspire add` doesn't support Central Package Management, causing confusion.

**Check logic:**
- Detect Directory.Packages.props in solution
- Warn about CPM limitations with certain CLI commands

---

### 2.5 AppHost SDK Package Availability
**Priority:** Medium
**Source:** [#6622](https://github.com/dotnet/aspire/issues/6622)

Aspire orchestration fails if the AppHost SDK package isn't in the local NuGet cache.

**Check logic:**
- Parse AppHost .csproj for SDK version
- Check if SDK package exists in NuGet cache
- Warn if restore may be needed

**Fix suggestion:**
```
Run: dotnet restore
```

---

### 2.6 UserSecretsId Configuration
**Priority:** Medium
**Source:** General

AppHost projects require a `UserSecretsId` property for secrets to work (unless using single-file AppHost).

**Check logic:**
- Check if AppHost .csproj has `<UserSecretsId>` property
- Check if using single-file AppHost (which doesn't need it)
- Warn if missing and not single-file

**Fix suggestion:**
```
Add to AppHost .csproj: <UserSecretsId>your-guid-here</UserSecretsId>
Or run: dotnet user-secrets init
```

---

### 2.7 Referenced Projects Exist
**Priority:** Medium
**Source:** [#12851](https://github.com/dotnet/aspire/issues/12851)

AppHost fails with unclear errors if referenced projects don't exist.

**Check logic:**
- Parse AppHost .csproj for ProjectReference elements
- Verify each referenced .csproj file exists
- Report missing projects

---

## Category 3: Runtime-specific Checks

These checks require the AppHost to be running to gather diagnostic data. They can inspect resource configuration and test actual connectivity.

### 3.1 Azure Functions Core Tools Detection
**Priority:** High
**Source:** [#7010](https://github.com/dotnet/aspire/issues/7010)

Users adding Azure Functions to their Aspire projects get cryptic errors when `func` CLI is not installed or not in PATH.

**Error pattern:**
```
An error occurred trying to start process 'func' with working directory 'bin\Debug\net9.0\'.
The system cannot find the file specified.
```

**Check logic:**
- Detect Azure Functions resources in the running AppHost
- Check if `func` command is available in PATH
- Verify version meets minimum requirements

**Fix suggestion:**
```
Install Azure Functions Core Tools: npm i -g azure-functions-core-tools@4
Or install Azure development workload in Visual Studio
```

---

### 3.2 Node.js/npm Detection
**Priority:** Medium
**Source:** General

**Check logic:**
- Detect Node.js app resources in the running AppHost
- Check if `node` and `npm` are in PATH
- Verify minimum Node.js version (if applicable)

**Fix suggestion:**
```
Install Node.js from: https://nodejs.org/
```

---

### 3.3 Python Detection
**Priority:** Medium
**Source:** General

**Check logic:**
- Detect Python app resources in the running AppHost
- Check if `python` (or `python3`) is in PATH
- Check if required packages (uvicorn, etc.) are available

---

### 3.4 Port Conflict Detection
**Priority:** High
**Source:** [#12247](https://github.com/dotnet/aspire/issues/12247), [#8246](https://github.com/dotnet/aspire/issues/8246), [#6659](https://github.com/dotnet/aspire/issues/6659)

Fixed ports configured via `.WithEndpoint(port: N)` fail with "address already in use" errors.

**Error pattern:**
```
failed to set up container networking... failed to listen on TCP socket: address already in use
```

**Check logic:**
- Inspect resources with fixed port allocations
- Check if those ports are currently in use
- Identify which process is using the port

**Fix suggestion:**
```
Port {port} is in use by process {name} (PID: {pid})
Kill the process or use a different port
```

---

### 3.5 Container-to-Host Connectivity
**Priority:** High
**Source:** [#6547](https://github.com/dotnet/aspire/issues/6547), [#8286](https://github.com/dotnet/aspire/issues/8286), [#12615](https://github.com/dotnet/aspire/issues/12615)

Containers can't reach services running on the host, especially with Docker Engine (not Desktop) or Podman.

**Check logic:**
- Detect if container resources exist
- Test connectivity from container to host services
- Verify `ASPIRE_ENABLE_CONTAINER_TUNNEL` is set for Docker Engine

**Fix suggestion:**
```
Set environment variable: ASPIRE_ENABLE_CONTAINER_TUNNEL=true
```

---

### 3.6 Health Check Status Monitoring
**Priority:** Medium
**Source:** [#10995](https://github.com/dotnet/aspire/issues/10995), [#11550](https://github.com/dotnet/aspire/issues/11550)

Containers stuck in "unhealthy" state, especially on ARM64 where health checks use IPv6 but containers default to IPv4.

**Check logic:**
- Monitor resources that stay unhealthy for extended time
- Check if health check endpoints are reachable
- Detect IPv4/IPv6 mismatches

---

### 3.7 Telemetry Pipeline Validation
**Priority:** Low
**Source:** [#6635](https://github.com/dotnet/aspire/issues/6635)

Podman containers sometimes don't send telemetry to the dashboard.

**Check logic:**
- Verify OTEL endpoints are reachable from containers
- Check if telemetry is flowing to dashboard

---

### 3.8 Resource Startup Timeout Detection
**Priority:** Low
**Source:** [#12241](https://github.com/dotnet/aspire/issues/12241)

DCP service creation can timeout on slow machines or with many resources.

**Check logic:**
- Monitor resource startup times
- Warn if approaching timeout thresholds

---

### 3.9 PostgreSQL Volume Authentication Issues
**Priority:** Medium
**Source:** [#11337](https://github.com/dotnet/aspire/issues/11337)

PostgreSQL containers fail with SCRAM authentication errors when reusing volumes with changed passwords.

**Error pattern:**
```
FATAL: password authentication failed for user "postgres"
```

**Check logic:**
- Detect PostgreSQL resources with persistent volumes
- Check if password in secrets matches what's in the volume
- Warn about potential auth mismatch

**Fix suggestion:**
```
Delete the PostgreSQL volume and restart, or ensure password hasn't changed
```

---

### 3.10 SQL Server Volume Permission Issues
**Priority:** Medium
**Source:** [#7182](https://github.com/dotnet/aspire/issues/7182), [#12763](https://github.com/dotnet/aspire/issues/12763), [#5055](https://github.com/dotnet/aspire/issues/5055)

SQL Server containers fail with permission errors on bind mounts, especially on macOS/ARM64.

**Check logic:**
- Detect SQL Server resources with volumes
- Check for permission-related errors in resource logs
- Detect ARM64 architecture issues

---

### 3.11 Project Build Status
**Priority:** Medium
**Source:** [#2154](https://github.com/dotnet/aspire/issues/2154)

Projects referenced via path (not solution) may not be built, causing startup failures.

**Check logic:**
- Detect projects added via path
- Check if project output exists
- Warn if build may be needed

---

### 3.12 Bind Mount Path Issues (WSL)
**Priority:** Medium
**Source:** [#5378](https://github.com/dotnet/aspire/issues/5378), [#3945](https://github.com/dotnet/aspire/issues/3945)

Bind mounts fail when paths don't translate correctly between Windows and WSL/Linux.

**Check logic:**
- Detect WSL environment
- Check bind mount paths for Windows-style paths
- Warn about cross-OS path issues

---

## Summary: Implementation Priority

### Machine-wide Checks (22)
| Priority | Check | Source |
|----------|-------|--------|
| High | Deprecated Aspire Workload | [#11202](https://github.com/dotnet/aspire/issues/11202) |
| High | Conflicting Environment Variables | [#5389](https://github.com/dotnet/aspire/issues/5389) |
| High | Group Policy Blocking Aspire Tools | [#6855](https://github.com/dotnet/aspire/issues/6855) |
| High | Corporate Certificate Policy | [#7443](https://github.com/dotnet/aspire/issues/7443) |
| Medium | IDE Certificate Expiry | [#6207](https://github.com/dotnet/aspire/issues/6207) |
| Medium | Container Runtime (Enhanced) | [#10199](https://github.com/dotnet/aspire/issues/10199) |
| Medium | Docker Windows Container Mode | [#11389](https://github.com/dotnet/aspire/issues/11389) |
| Medium | Docker Desktop Resource Saver Mode | [Discussion #9739](https://github.com/dotnet/aspire/discussions/9739) |
| Medium | NuGet Connectivity | [Discussion #12518](https://github.com/dotnet/aspire/discussions/12518) |
| Medium | Docker Snap Installation (Linux) | [#11027](https://github.com/dotnet/aspire/issues/11027) |
| Medium | Docker Desktop Slow Response | [#7802](https://github.com/dotnet/aspire/issues/7802) |
| Medium | Proxy Environment Variables | [#1012](https://github.com/dotnet/aspire/issues/1012) |
| Medium | IPv6 Localhost Availability | [#3355](https://github.com/dotnet/aspire/issues/3355) |
| Medium | HTTP/2 Proxy Interference | [#1818](https://github.com/dotnet/aspire/issues/1818) |
| Medium | CI/CD Environment Dev Certs | [#9061](https://github.com/dotnet/aspire/issues/9061) |
| Low | Docker Proxy Configuration | [#11413](https://github.com/dotnet/aspire/issues/11413) |
| Low | Temp Directory Permissions | [Discussion #1671](https://github.com/dotnet/aspire/discussions/1671) |
| Low | localhost DNS Resolution | [#10754](https://github.com/dotnet/aspire/issues/10754) |
| Low | Windows Dev Drive Configuration | [#10268](https://github.com/dotnet/aspire/issues/10268) |
| Low | Corporate Network/VPN Detection | [#9376](https://github.com/dotnet/aspire/issues/9376) |
| Low | Dev Container Configuration | [#6830](https://github.com/dotnet/aspire/issues/6830) |
| Low | OTEL Environment Variable Conflicts | [#6939](https://github.com/dotnet/aspire/issues/6939) |

### Project-specific Checks (7)
| Priority | Check | Source |
|----------|-------|--------|
| Medium | Dashboard Port Availability | [#3044](https://github.com/dotnet/aspire/issues/3044) |
| Medium | Aspire SDK Version Consistency | [#12274](https://github.com/dotnet/aspire/issues/12274) |
| Medium | launchSettings.json Validation | [#7975](https://github.com/dotnet/aspire/issues/7975) |
| Medium | AppHost SDK Package Availability | [#6622](https://github.com/dotnet/aspire/issues/6622) |
| Medium | Referenced Projects Exist | [#12851](https://github.com/dotnet/aspire/issues/12851) |
| Medium | UserSecretsId Configuration | General |
| Low | Central Package Management Detection | [#13241](https://github.com/dotnet/aspire/issues/13241) |

### Runtime-specific Checks (12)
| Priority | Check | Source |
|----------|-------|--------|
| High | Azure Functions Core Tools | [#7010](https://github.com/dotnet/aspire/issues/7010) |
| High | Port Conflict Detection | [#12247](https://github.com/dotnet/aspire/issues/12247) |
| High | Container-to-Host Connectivity | [#6547](https://github.com/dotnet/aspire/issues/6547) |
| Medium | Node.js/npm Detection | General |
| Medium | Python Detection | General |
| Medium | Health Check Monitoring | [#10995](https://github.com/dotnet/aspire/issues/10995) |
| Medium | PostgreSQL Volume Auth Issues | [#11337](https://github.com/dotnet/aspire/issues/11337) |
| Medium | SQL Server Volume Permissions | [#7182](https://github.com/dotnet/aspire/issues/7182) |
| Medium | Project Build Status | [#2154](https://github.com/dotnet/aspire/issues/2154) |
| Medium | Bind Mount Path Issues (WSL) | [#5378](https://github.com/dotnet/aspire/issues/5378) |
| Low | Telemetry Pipeline Validation | [#6635](https://github.com/dotnet/aspire/issues/6635) |
| Low | Resource Startup Timeout | [#12241](https://github.com/dotnet/aspire/issues/12241) |

---

## Existing Checks (for reference)

| Check | Category | What it validates |
|-------|----------|-------------------|
| WSL Environment | Machine-wide | WSL1 vs WSL2 detection |
| .NET SDK | Machine-wide | SDK version requirement |
| Dev Certificates | Machine-wide | HTTPS cert trusted |
| Container Runtime | Machine-wide | Docker ≥28.0 / Podman ≥5.0 |

---

## GitHub Issues Analyzed

### Certificate/TLS Issues
- [#7443](https://github.com/dotnet/aspire/issues/7443) - SSL error running starter project
- [#6207](https://github.com/dotnet/aspire/issues/6207) - IDE certificate expired
- [#5548](https://github.com/dotnet/aspire/issues/5548) - IDE cert expires after 7 days
- [#9158](https://github.com/dotnet/aspire/issues/9158) - SSL authentication exception

### Container/Docker Issues
- [#10199](https://github.com/dotnet/aspire/issues/10199) - Rancher Desktop support
- [#12595](https://github.com/dotnet/aspire/issues/12595) - containerd image store for multi-arch
- [#6635](https://github.com/dotnet/aspire/issues/6635) - Podman telemetry issues
- [#12998](https://github.com/dotnet/aspire/issues/12998) - Dashboard fails with Docker not running
- [#11389](https://github.com/dotnet/aspire/issues/11389) - Docker Windows container mode detection
- [#11413](https://github.com/dotnet/aspire/issues/11413) - Docker proxy issues
- [#11027](https://github.com/dotnet/aspire/issues/11027) - Docker snap installation issues on Linux
- [#7802](https://github.com/dotnet/aspire/issues/7802) - Docker Desktop slow response/timeout
- [#10754](https://github.com/dotnet/aspire/issues/10754) - localhost vs 127.0.0.1 resolution
- [#6830](https://github.com/dotnet/aspire/issues/6830) - Dev container docker-outside-of-docker not supported

### Port Conflicts
- [#12247](https://github.com/dotnet/aspire/issues/12247) - "address already in use" with fixed ports
- [#8246](https://github.com/dotnet/aspire/issues/8246) - Codespaces port binding failures
- [#6659](https://github.com/dotnet/aspire/issues/6659) - Error gets double logged
- [#3044](https://github.com/dotnet/aspire/issues/3044) - Dashboard port conflict error improvement

### Azure Functions
- [#7010](https://github.com/dotnet/aspire/issues/7010) - `func` not found in PATH
- [#13205](https://github.com/dotnet/aspire/issues/13205) - aspire run fails for Azure Functions

### Workload/SDK Issues
- [#11202](https://github.com/dotnet/aspire/issues/11202) - Enhance aspire update to fix environment
- [#12274](https://github.com/dotnet/aspire/issues/12274) - VS requires Desktop Experience workload (SDK version issue)
- [#11458](https://github.com/dotnet/aspire/issues/11458) - aspire update doesn't fully upgrade SDK
- [#6622](https://github.com/dotnet/aspire/issues/6622) - SDK not in local cache

### Container-Host Connectivity
- [#6547](https://github.com/dotnet/aspire/issues/6547) - Container to host connection issues
- [#8286](https://github.com/dotnet/aspire/issues/8286) - Can't reach docker container services
- [#12615](https://github.com/dotnet/aspire/issues/12615) - YARP container can't proxy to host

### Volume/Persistence Issues
- [#11337](https://github.com/dotnet/aspire/issues/11337) - PostgreSQL SCRAM auth with existing volumes
- [#7182](https://github.com/dotnet/aspire/issues/7182) - SQL Server volume access denied
- [#12763](https://github.com/dotnet/aspire/issues/12763) - SQL Server permissions on Apple Silicon
- [#5055](https://github.com/dotnet/aspire/issues/5055) - SQL Server bind mount failures
- [#5378](https://github.com/dotnet/aspire/issues/5378) - Bind mount path issues under WSL
- [#3945](https://github.com/dotnet/aspire/issues/3945) - Bind mount broken on Fedora/RHEL

### Project/Build Issues
- [#2154](https://github.com/dotnet/aspire/issues/2154) - Project not built when added via path
- [#12851](https://github.com/dotnet/aspire/issues/12851) - Unclear error if project doesn't exist
- [#7975](https://github.com/dotnet/aspire/issues/7975) - launchSettings.json trailing comma issues
- [#8795](https://github.com/dotnet/aspire/issues/8795) - All profiles validated even if not used

### Health Check Issues
- [#10995](https://github.com/dotnet/aspire/issues/10995) - Containers stuck in unhealthy state
- [#11550](https://github.com/dotnet/aspire/issues/11550) - ARM64 IPv6/IPv4 health check mismatch
- [#12241](https://github.com/dotnet/aspire/issues/12241) - DCP service creation timeout

### Environment/Machine Configuration Issues
- [#10268](https://github.com/dotnet/aspire/issues/10268) - Windows Dev Drive mounted as folder breaks DCP
- [#9376](https://github.com/dotnet/aspire/issues/9376) - Corporate VPN/security software interference
- [#10391](https://github.com/dotnet/aspire/issues/10391) - Docker health check not resilient
- [#9709](https://github.com/dotnet/aspire/issues/9709) - DCP doesn't run under Alpine (musl libc)

### Environment Variable Issues
- [#5389](https://github.com/dotnet/aspire/issues/5389) - `name` and `shell` env vars break VS Code on WSL
- [#5475](https://github.com/dotnet/aspire/issues/5475) - Long env vars (30k+ chars) cause IDE timeout
- [#1012](https://github.com/dotnet/aspire/issues/1012) - HTTP_PROXY/HTTPS_PROXY breaks localhost connections
- [#3355](https://github.com/dotnet/aspire/issues/3355) - VPN blocking IPv6 localhost causes startup failure
- [#6939](https://github.com/dotnet/aspire/issues/6939) - OTEL_EXPORTER_OTLP_PROTOCOL conflicts with Seq
- [#13219](https://github.com/dotnet/aspire/issues/13219) - SSL_CERT_DIR overwritten breaks TLS
- [#5022](https://github.com/dotnet/aspire/issues/5022) - All host env vars leak when none specified

### Corporate/VPN/Proxy Issues
- [#6855](https://github.com/dotnet/aspire/issues/6855) - Group policy blocks Aspire.RuntimeIdentifier.Tool
- [#7443](https://github.com/dotnet/aspire/issues/7443) - Corporate policy blocks self-signed certificates
- [#9158](https://github.com/dotnet/aspire/issues/9158) - SSL authentication exception on VDI
- [#1818](https://github.com/dotnet/aspire/issues/1818) - HTTP/2 proxy breaks OTEL connections
- [#9061](https://github.com/dotnet/aspire/issues/9061) - GitHub Actions SSL client error
- [#3670](https://github.com/dotnet/aspire/issues/3670) - Standalone dashboard cert validation
- [#10633](https://github.com/dotnet/aspire/issues/10633) - Custom Kestrel certs break AppHost
- [Discussion #13629](https://github.com/dotnet/aspire/discussions/13629) - Aspire on corporate Azure Virtual Desktop

---

## GitHub Discussions Analyzed

### Environment/Setup Discussions
- [#9739](https://github.com/dotnet/aspire/discussions/9739) - Docker "unhealthy" with Resource Saver mode
- [#12518](https://github.com/dotnet/aspire/discussions/12518) - Slow restore times / NuGet issues
- [#1671](https://github.com/dotnet/aspire/discussions/1671) - Run session temp file failures
- [#13453](https://github.com/dotnet/aspire/discussions/13453) - Starting Aspire in WSL
- [#13429](https://github.com/dotnet/aspire/discussions/13429) - Python in WSL, ASP.NET in Windows

### Configuration/Runtime Discussions
- [#12338](https://github.com/dotnet/aspire/discussions/12338) - OTEL exporter failures silently swallowed
- [#6989](https://github.com/dotnet/aspire/discussions/6989) - OpenAPI port mismatch (proxy issue)
- [#7126](https://github.com/dotnet/aspire/discussions/7126) - DistributedApplicationTestingBuilder frustrations
- [#3516](https://github.com/dotnet/aspire/discussions/3516) - RabbitMQ container connection errors
- [#1615](https://github.com/dotnet/aspire/discussions/1615) - "Failed to bind to address" issues

### Deployment Discussions
- [#1434](https://github.com/dotnet/aspire/discussions/1434) - AZD UP InvalidParameterValue errors
- [#12652](https://github.com/dotnet/aspire/discussions/12652) - Multiple environments guidance
- [#11850](https://github.com/dotnet/aspire/discussions/11850) - aspire deploy feedback
