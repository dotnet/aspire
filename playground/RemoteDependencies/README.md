# Local application, SSH-managed remote Redis (experimental)

Working design exploration for [discussion #18577](https://github.com/microsoft/aspire/discussions/18577).
This is **remote dependency development, not deployment of the application**.
The AppHost and ASP.NET Core API run locally; Aspire creates Redis in a different
Linux environment through SSH, forwards its endpoint, streams its logs and owns
its lifetime. No global `DOCKER_HOST` and no change to shipped hosting APIs.

The bounded journey is one ephemeral Redis dependency consumed by a **local
project** through `WithReference` and `WaitFor`. This deliberately does not claim
`AddRedis().RunOnHost(...)` support for arbitrary existing container resources.

## Reproduce

Prerequisites: Linux, OpenSSH client/keygen, Python 3, Docker supporting privileged
Linux containers, the repository's pinned SDK and .NET 10 runtime. Use only a
disposable development machine for the privileged fixture.

From the repository root:

```sh
./restore.sh
./.dotnet/dotnet build playground/RemoteDependencies/RemoteDependencies.AppHost \
    -p:SkipDashboardProjectReference=true
docker info
python3 playground/RemoteDependencies/fixture.py up
. playground/RemoteDependencies/.fixture/env.sh
REMOTE_HEADLESS=1 ./.dotnet/dotnet \
    artifacts/bin/RemoteDependencies.AppHost/Debug/net10.0/RemoteDependencies.AppHost.dll
```

If `docker info` fails, start your normal local Docker daemon first. The fixture
never starts or replaces the workstation daemon. It creates a separate nested
engine with **only a Unix socket inside its container**, not a public TCP engine
listener, and does not mount the workstation Docker socket.

Direct DLL execution avoids the source-tree AppHost package's CLI-bundle run
hook. Build first. For this sandbox, `./restore.sh` installed the SDK but the full
solution restore hit unrelated feed download errors; targeted builds succeeded
with `-p:NuGetAudit=false` because the configured audit endpoint was unavailable.
That command-line workaround is not a repository feed/configuration change.

In another terminal:

```sh
curl --fail http://127.0.0.1:18080/
curl --fail http://127.0.0.1:18080/
```

Responses include incrementing `requests`, `"cache":"127.0.0.1:16379"` and
`"application":"local"`. The cache value is injected by Aspire, not hardcoded in
the API. Ctrl+C the AppHost to cancel its sessions and await owned-container
cleanup. To run the automated manual scenario matrix instead, stop that AppHost:

```sh
python3 playground/RemoteDependencies/verify.py
```

The verifier uses the built AppHost, real SSH, real Docker and real Redis; it
does not substitute a fake transport or fake dependency. It reserves local ports
18080 and 16379 for the duration of the run. Do not run parallel copies.

Run proportional unit/model/protocol coverage:

```sh
./.dotnet/dotnet test \
    --project tests/Aspire.Hosting.RemoteDependencies.Tests \
    --no-launch-profile -- \
    --filter-not-trait 'quarantined=true' --filter-not-trait 'outerloop=true'
```

Add `-p:NuGetAudit=false` before `--` only if the audit endpoint is inaccessible.
These tests are normal PR-CI tests; the privileged fixture and `verify.py` are
explicitly manual, not an implicit Docker requirement for unit CI.

## Topology and access

```text
developer environment
  AppHost + local ASP.NET API :18080
    ConnectionStrings__cache = 127.0.0.1:16379
      OpenSSH local forward (bind 127.0.0.1 only)
        SSH fixture container :22 (published on workstation loopback, random port)
          remote network namespace: 127.0.0.1:26379
            nested Docker Redis container (--network host, bind 127.0.0.1)
```

The fixture generates **new task-owned keys** under ignored `.fixture/` (directory
mode 0700); no existing SSH keys are read. It pins the server public host key via
the local Docker control channel, not unauthenticated `ssh-keyscan`. Password,
keyboard-interactive, agent, X11 and remote forwarding are disabled. Local
forwarding is restricted to remote IPv4 loopback destinations. Use
`.fixture/env.sh` for the generated SSH port and key paths; do not commit it.

`fixture.py up` sets `REMOTE_HOST_NETWORK_PORT=26379`. This is an explicit
**Linux host-network fallback inside the isolated remote fixture**, not host
networking with the developer workstation. Nested bridge traffic was unavailable
in this sandbox: Docker published a port and Redis started, but the nested
bridge path timed out. With the fallback, local and remote loopbacks really are
different network namespaces and the live requests traverse SSH.

Without `REMOTE_HOST_NETWORK_PORT`, the prototype uses ordinary Docker
`--publish 127.0.0.1::6379`, inspects the allocated remote port and tunnels to it.
That bridge path has parsing/model coverage but **has not passed end-to-end
runtime validation here**. The verified supported reproduction is the fixture's
host-network journey. This is one physical developer environment, **not WAN,
latency, packet-loss, security-isolation or production-host validation**.

For a separately provisioned authorized development host, supply `REMOTE_HOST`,
`REMOTE_SSH_PORT`, `REMOTE_USER`, `REMOTE_IDENTITY`, `REMOTE_KNOWN_HOSTS` and,
for the same tested Linux host-network journey, `REMOTE_HOST_NETWORK_PORT`.
The remote user needs Docker CLI/daemon access and permission to forward TCP.
Select unused remote/local ports (`REMOTE_LOCAL_PORT` defaults to 16379).
No host address, identity or per-developer credential is checked into the model.

## Lifecycle, observability and cleanup

The resource uses repository-standard `OnInitializeResource`,
`BeforeResourceStartedEvent`, allocated endpoint/connection-string events,
`ResourceNotificationService`, `ResourceLoggerService`, relationships and
`WaitFor`. An explicitly owned hosted service awaits session cleanup on shutdown.
The endpoint describes the **local tunnel**, including its local target port,
so DCP does not allocate a conflicting proxy or give local projects a remote
loopback address. Readiness requires accepted SSH forwarding, a protocol-level
Redis PONG and a running owned container, not merely an open TCP socket.

`REMOTE_HEADLESS=1` subscribes to the same notification/log services used by the
dashboard and prints JSON state/log records. Redis startup output, local/remote
addresses, owner/container identities, errors and cleanup commands are observable
there. Without that variable the model uses normal dashboard services; the
dashboard UI itself was not built or visually validated for this experiment.
The machine resource's state is a startup SSH/Docker preflight check, not a
continuous global host-health probe; the cache session is continuously monitored.

| Event | Behavior |
|---|---|
| Bad SSH/host key/engine | Machine and dependent resource fail explicitly; no local dependency fallback. |
| Bad image or occupied local forward port | `FailedToStart`; local API does not pass its wait gate; cleanup runs. |
| Tunnel/log session exits or Redis PING fails | `Unavailable`, inactive endpoint, local API returns HTTP 503; no silent reconnect. |
| SSH restored | Restart AppHost explicitly; a new owner/container/session is created. |
| Graceful AppHost cancellation | Kill only its SSH process; await label-checked removal of its exact container ID. |
| Remote host unreachable during cleanup | Log error and exact reconciliation command; attach `Cleanup required` property instead of claiming success. |
| SSH/cancellation interrupts remote creation | Creation may still settle remotely; even a successful cleanup snapshot is reported unconfirmed and requires reconciliation. |
| AppHost killed without shutdown | No durable lease/remote janitor; use the recorded owner/name to reconcile manually. |

Each run uses a random container name and `aspire.remote.owner` label. Cleanup
looks up that name, verifies the owner and removes the **captured immutable ID**,
not all containers, a global prefix, or a reused name. The integration verifier
also creates an unrelated sentinel container and proves it survives cleanup.

After the checks, remove the fixture and its nested workloads, anonymous Docker
data volume, generated image and ephemeral key files:

```sh
python3 playground/RemoteDependencies/fixture.py down
```

Only the container ID recorded in `.fixture/state.json` is eligible for fixture
removal, and its task label must match. Scenario logs and `evidence.json` stay in
ignored `.fixture/` for inspection; their generated identity files are removed.
For a real remote host, run the **exact label-checked cleanup command** from the
cache logs/properties after SSH recovers. Do not substitute a broad `docker rm`.

## Evidence and design implications

[verification.json](verification.json) records the completed six-scenario run.
Nineteen focused tests pass, and the targeted AppHost/API build reports zero
warnings/errors. The scenario runner additionally checks the API's incremented
value directly inside Redis, native state transitions, log delivery, inactive
URLs, actual HTTP 503s, scoped cleanup and explicit restart recovery. It stops
the entire fixture for the remote-outage case, restores its engine, then
reconciles only the exact intentionally orphaned owned container.

This builds on the discussion's CLI-based runtime abstraction guidance rather
than introducing another engine API client. The existing `SshRemoteOptions`
detects an AppHost itself running under VS Code Remote SSH; it does not place
selected local-AppHost dependencies remotely. `Aspire.Hosting.RemoteHost` is the
polyglot AppHost RPC host, not SSH container placement.

The experiment suggests per-developer **placement/connection profiles** with
explicit endpoint-network identities, session ownership and lifecycle semantics
are more important than only adding a `RunOnHost` method. A product implementation
should extend the existing DCP runtime/connection abstraction, with equivalent
Docker and Podman behavior and an explicit unsupported-runtime result, rather
than silently bypassing DCP for arbitrary container resources.

Remaining boundaries: Docker/OpenSSH/Linux only; Redis only; one TCP endpoint;
local project consumers only; ephemeral containers/no persistent volumes; no
bind mounts, image builds/context transfer, arbitrary environment/secrets,
registry auth, remote TLS services, automatic reconnect, remote garbage
collection, dashboard restart/stop commands, or cross-machine health guarantees.
SSH host users and Docker access are trusted. Loopback Redis has no password;
other users on the remote host can access it. The privileged fixture and mutable
image tags are development conveniences, not a secure untrusted-image sandbox.
