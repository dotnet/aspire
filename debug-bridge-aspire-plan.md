# Implement 2026-02-01 Debug Bridge Protocol in dotnet/aspire

## TL;DR

DCP now supports a "debug bridge" mode (protocol version `2026-02-01`) where it launches debug adapters and proxies DAP messages through a Unix domain socket. Instead of VS Code launching its own debug adapter process, it connects to DCP's bridge socket, tells DCP which adapter to launch (via a length-prefixed JSON handshake), and then communicates DAP messages through that same socket. This requires changes to the IDE execution spec, the VS Code extension's session endpoint, debug adapter descriptor factory, and protocol capabilities.

Currently, `protocols_supported` tops out at `"2025-10-01"`. No `2026-02-01` or `debug_bridge` references exist anywhere in the aspire repo.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│ IDE (VS Code)                                                            │
│  └─ Debug Adapter Client                                                 │
│      └─ Connects to Unix socket provided by DCP in run session response  │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │ DAP messages (Unix socket)
                                   │ + Initial handshake (token + session ID + adapter config)
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ DCP DAP Bridge                                                           │
│  ├─ Shared Unix socket listener for IDE connections                      │
│  ├─ Handshake validation (token + session ID)                            │
│  ├─ Message forwarding (IDE ↔ Debug Adapter)                             │
│  ├─ Interception layer:                                                  │
│  │    ├─ initialize: ensure supportsRunInTerminalRequest = true          │
│  │    ├─ runInTerminal: handle locally, launch process, capture stdio    │
│  │    └─ output events: capture for logging (unless runInTerminal used)  │
│  └─ Process runner for runInTerminal commands                            │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │ DAP messages (stdio/TCP)
                                   ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ Debug Adapter (launched by DCP)                                          │
│  └─ coreclr, debugpy, etc.                                               │
└──────────────────────────────────────────────────────────────────────────┘
```

### How It Differs from the Current Flow

| Aspect | Current (no bridge) | New (bridge mode, 2026-02-01+) |
|--------|---------------------|-------------------------------|
| Who launches the debug adapter | VS Code (via `vscode.debug.startDebugging`) | DCP (via bridge, using config from IDE) |
| DAP transport | VS Code manages directly | Unix socket through DCP bridge |
| `runInTerminal` handling | VS Code handles | DCP handles locally (IDE never sees it) |
| stdout/stderr capture | Adapter tracker sends `serviceLogs` | DCP captures from process pipes or output events |
| IDE role | Full debug orchestrator | DAP client connected through bridge socket |

---

## Step-by-Step Implementation

### Step 1: Update the IDE Execution Spec

**File:** `docs/specs/IDE-execution.md`

Add the `2026-02-01` protocol version under **Protocol Versioning → Well-known protocol versions**:

> **`2026-02-01`**
> Changes:
> - Adds debug bridge support. When this version (or later) is negotiated, the `PUT /run_session` payload may include `debug_bridge_socket_path` and `debug_session_id` fields.

Add the two new fields to the **Create Session Request** payload documentation:

| Property | Description | Type |
|----------|-------------|------|
| `debug_bridge_socket_path` | Unix domain socket path that the IDE should connect to for DAP bridging. Present only when API version ≥ `2026-02-01`. | `string` (optional) |
| `debug_session_id` | A unique session identifier the IDE must include in the debug bridge handshake. | `string` (optional) |

Add a new section **"Debug Bridge Protocol"** describing the full protocol (see [Appendix A](#appendix-a-debug-bridge-protocol-specification) below for the complete spec text).

---

### Step 2: Update Protocol Capabilities

**File:** `extension/src/capabilities.ts` (~line 55)

Add `"2026-02-01"` to the `protocols_supported` array:

```ts
export function getRunSessionInfo(): RunSessionInfo {
    return {
        protocols_supported: ["2024-03-03", "2024-04-23", "2025-10-01", "2026-02-01"],
        supported_launch_configurations: getSupportedCapabilities()
    };
}
```

---

### Step 3: Update TypeScript Types

**File:** `extension/src/dcp/types.ts`

Add the new fields to the run session payload type, and add new types for the handshake:

```ts
// Add to existing RunSessionPayload (or equivalent) interface:
debug_bridge_socket_path?: string;
debug_session_id?: string;

// New types for the bridge protocol:
export interface DebugAdapterConfig {
    args: string[];
    mode?: "stdio" | "tcp-callback" | "tcp-connect";
    env?: Array<{ name: string; value: string }>;
    connectionTimeoutSeconds?: number;
}

```

Note: The handshake types (`DebugBridgeHandshakeRequest`, `DebugBridgeHandshakeResponse`) are not needed in the extension — the Aspire debug adapter CLI middleware handles the bridge handshake internally.

---

### Step 4: Map Launch Configuration Types to Debug Adapter Configs

The `debug_adapter_config` in the handshake tells DCP what debug adapter binary to launch. The IDE must determine this from the launch configuration type.

The mapping information already exists in `extension/src/debugger/debuggerExtensions.ts` and the language-specific files:

| Launch Config Type | Debug Adapter | Source Extension |
|-------------------|---------------|-----------------|
| `project` | `coreclr` | `ms-dotnettools.csharp` |
| `python` | `debugpy` | `ms-python.python` |

Add a method to `ResourceDebuggerExtension` (or a standalone utility) that returns a `DebugAdapterConfig`:

```ts
export interface ResourceDebuggerExtension {
    // ... existing fields ...
    getDebugAdapterConfig?(launchConfig: LaunchConfiguration): DebugAdapterConfig;
}
```

For each resource type:
- **`project` / `coreclr`**: Resolve the path to the C# debug adapter executable from the `ms-dotnettools.csharp` extension. Set `mode: "stdio"`. The `args` array should be the command line to launch the adapter (e.g., `["/path/to/Microsoft.CodeAnalysis.LanguageServer", "--debug"]` or whatever the coreclr adapter binary is).
- **`python` / `debugpy`**: Resolve the path to the debugpy adapter. Set `mode: "stdio"` or `"tcp-connect"` as appropriate. For `"tcp-connect"`, use `{{port}}` as a placeholder in `args` — DCP will replace it with an actual port number.

This is the **key integration point** — the extension needs to locate the actual debug adapter binary that would normally be launched by VS Code's built-in debug infrastructure and package it as an `args` array for the handshake.

---

### Step 5: Update `PUT /run_session` Handler

**File:** `extension/src/dcp/AspireDcpServer.ts` (~lines 84-120)

Modify the `PUT /run_session` handler:

```
Parse request body
  ↓
Extract debug_bridge_socket_path and debug_session_id
  ↓
┌─ If BOTH fields are present (bridge mode):
│   1. Resolve DebugAdapterConfig for the launch configuration type (Step 4)
│   2. Pass bridge socket path, session ID, and adapter config to the CLI middleware
│   3. CLI middleware handles the handshake and DAP proxying
│   4. Register the bridge session in BridgeSessionRegistry
│   5. Start a VS Code debug session using the bridge adapter
│   6. Respond 201 Created + Location header
│
└─ If fields are ABSENT (legacy mode):
    Follow existing flow unchanged
```

---

### Step 6: Create a Bridge Debug Adapter

**New file:** `extension/src/debugger/debugBridgeAdapter.ts`

Create a custom `vscode.DebugAdapter` that proxies DAP messages to/from the connected Unix socket:

```ts
export class DebugBridgeAdapter implements vscode.DebugAdapter {
    private sendMessage: vscode.EventEmitter<vscode.DebugProtocolMessage>;
    onDidSendMessage: vscode.Event<vscode.DebugProtocolMessage>;

    constructor(private socket: net.Socket) { ... }

    // Called by VS Code when it wants to send a DAP message to the adapter
    handleMessage(message: vscode.DebugProtocolMessage): void {
        // Write as DAP-framed message (Content-Length header + JSON) to the socket
    }

    // Read DAP-framed messages from the socket and emit via onDidSendMessage

    dispose(): void {
        // Close the socket
    }
}
```

**Why not `DebugAdapterNamedPipeServer`?** The Aspire debug adapter CLI middleware manages the bridge handshake and provides an already-connected socket to the extension. The inline adapter approach gives full control over the connection lifecycle.

Then update `AspireDebugAdapterDescriptorFactory` to return a `DebugAdapterInlineImplementation` wrapping this adapter for bridge sessions:

```ts
return new vscode.DebugAdapterInlineImplementation(new DebugBridgeAdapter(connectedSocket));
```

---

### Step 7: Update Debug Session Lifecycle

**File:** `extension/src/debugger/AspireDebugSession.ts`

For bridge-mode sessions:
- The `launch` request handler should **not** spawn `aspire run --start-debug-session` (DCP already manages the process)
- Track whether this is a bridge session (e.g., via a flag or session metadata)
- On `disconnect`/`terminate`, close the bridge socket connection
- Teardown should notify DCP via the existing WebSocket notification path (`sessionTerminated`)

---

### Step 8: Update Adapter Tracker for Bridge Sessions

**File:** `extension/src/debugger/adapterTracker.ts`

For bridge sessions:
- DCP captures stdout/stderr directly from the debug adapter's output events and from `runInTerminal` process pipes — the tracker should **not** send duplicate `serviceLogs` notifications for output that DCP is already capturing
- The tracker should still send `processRestarted` and `sessionTerminated` notifications
- Consider skipping tracker registration entirely for bridge sessions, or adding a bridge-mode flag that suppresses log forwarding

---

### Step 9: Update C# Models (if needed)

**Files in:** `src/Aspire.Hosting/Dcp/Model/`

If the app host or dashboard reads the run session payload structure, update any C# deserialization models to include the new optional fields for forward compatibility. Check:
- `RunSessionInfo.cs`
- Any request/response models that mirror the run session payload

This may not be strictly necessary if the C# side doesn't interact with these fields — DCP adds them server-side. But it's good practice for model completeness.

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| **Inline adapter over named pipe descriptor** | The CLI middleware handles the handshake and provides an already-connected socket, so we use a `DebugAdapterInlineImplementation` wrapping a custom adapter that manages the socket lifecycle |
| **Token reuse** | The same bearer token used for HTTP authentication (`DEBUG_SESSION_TOKEN`) is reused as the bridge handshake token — no new credential needed |
| **IDE decides adapter** | DCP does NOT tell the IDE which adapter to use; the IDE determines this from the launch configuration type and sends the adapter binary path + args back in the handshake's `debug_adapter_config` |
| **Backward compatible** | When `debug_bridge_socket_path` is absent from the run session request, the existing non-bridge flow is used unchanged |

---

## Verification

1. **Integration test**: Start a DCP instance with debug bridge enabled, verify the extension:
   - Reports `"2026-02-01"` in `protocols_supported`
   - Passes bridge socket path and session ID to the CLI middleware
   - CLI middleware performs the handshake and establishes the bridge
   - Successfully forwards DAP messages through the bridge
3. **Regression**: Ensure the existing (non-bridge) flow still works when DCP negotiates an older API version
4. **Manual test**: Debug a .NET Aspire app with the updated extension and verify breakpoints, stepping, variable inspection all work through the bridge

---

## Appendix A: Debug Bridge Protocol Specification

### Overview

When API version `2026-02-01` or later is negotiated, DCP may include debug bridge fields in the `PUT /run_session` request. When present, the IDE should connect to the provided Unix domain socket and use DCP as a DAP bridge instead of launching its own debug adapter.

### Connection Flow

1. IDE receives `PUT /run_session` with `debug_bridge_socket_path` and `debug_session_id`
2. IDE responds `201 Created` with `Location` header (as normal)
3. IDE connects to the Unix domain socket at `debug_bridge_socket_path`
4. IDE sends a handshake request (length-prefixed JSON)
5. DCP validates and responds with a handshake response
6. On success, standard DAP messages flow over the same socket connection
7. DCP launches the debug adapter specified in the handshake and bridges messages bidirectionally

### Handshake Wire Format

All handshake messages use **length-prefixed JSON**:
```
[4 bytes: big-endian uint32 payload length][JSON payload bytes]
```

Maximum message size: **65536 bytes** (64 KB).

### Handshake Request (IDE → DCP)

```json
{
    "token": "<same bearer token used for HTTP auth (DEBUG_SESSION_TOKEN)>",
    "session_id": "<debug_session_id from the run session request>",
    "debug_adapter_config": {
        "args": ["/path/to/debug-adapter", "--arg1", "value1"],
        "mode": "stdio",
        "env": [
            { "name": "VAR_NAME", "value": "var_value" }
        ],
        "connectionTimeoutSeconds": 10
    }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `token` | `string` | Yes | The same bearer token used for HTTP authentication |
| `session_id` | `string` | Yes | The `debug_session_id` from the run session request |
| `debug_adapter_config` | `object` | Yes | Configuration for launching the debug adapter |
| `debug_adapter_config.args` | `string[]` | Yes | Command + arguments to launch the adapter. First element is the executable path. |
| `debug_adapter_config.mode` | `string` | No | `"stdio"` (default), `"tcp-callback"`, or `"tcp-connect"` |
| `debug_adapter_config.env` | `array` | No | Environment variables as `[{"name":"N","value":"V"}]` |
| `debug_adapter_config.connectionTimeoutSeconds` | `number` | No | Timeout for TCP connections (default: 10 seconds) |

### Debug Adapter Modes

| Mode | Description |
|------|-------------|
| `stdio` (default) | DCP launches the adapter and communicates via stdin/stdout |
| `tcp-callback` | DCP starts a TCP listener, then launches the adapter. The adapter connects back to DCP. |
| `tcp-connect` | DCP allocates a port, replaces `{{port}}` placeholder in `args`, launches the adapter (which listens on that port), then DCP connects to it. |

### Handshake Response (DCP → IDE)

Success:
```json
{
    "success": true
}
```

Failure:
```json
{
    "success": false,
    "error": "error description"
}
```

### Handshake Validation

DCP validates the handshake in this order:
1. Token matches the registered session token → otherwise `"invalid session token"`
2. Session ID exists → otherwise `"bridge session not found"`
3. `debug_adapter_config` is present → otherwise `"debug adapter configuration is required"`
4. Session not already connected → otherwise `"session already connected"` (only one IDE connection per session allowed)

### Timeouts

| Timeout | Duration | Description |
|---------|----------|-------------|
| Handshake | 30 seconds | DCP closes the connection if the handshake request isn't received within this time |
| Adapter connection (TCP modes) | 10 seconds (configurable) | Time to establish TCP connection to/from adapter |

### DAP Message Flow After Handshake

After a successful handshake, standard DAP messages flow over the Unix socket using the standard DAP wire format (`Content-Length: N\r\n\r\n{JSON}`).

DCP intercepts the following messages:
- **`initialize` request** (IDE → Adapter): DCP forces `supportsRunInTerminalRequest = true` in the arguments before forwarding
- **`runInTerminal` reverse request** (Adapter → IDE): DCP handles this locally by launching the process. The IDE will **never** receive `runInTerminal` requests.
- **`output` events** (Adapter → IDE): DCP captures these for logging purposes, then forwards to the IDE

All other DAP messages are forwarded transparently in both directions.

### Output Capture

| Scenario | stdout/stderr source | Output events |
|----------|---------------------|---------------|
| No `runInTerminal` | Captured from DAP `output` events | Logged by DCP + forwarded to IDE |
| With `runInTerminal` | Captured from process pipes by DCP | Forwarded to IDE (not logged from events) |

---

## Appendix B: Relevant DCP Source Files

These files in the `microsoft/dcp` repo implement the DCP side of the bridge protocol, for reference:

| File | Purpose |
|------|---------|
| `internal/dap/bridge.go` | Core `DapBridge` — bidirectional message forwarding with interception |
| `internal/dap/bridge_handshake.go` | Length-prefixed JSON handshake protocol implementation |
| `internal/dap/bridge_session.go` | `BridgeSessionManager` — session registry, state tracking |
| `internal/dap/bridge_socket_manager.go` | `BridgeSocketManager` — shared Unix socket listener, dispatches connections |
| `internal/dap/adapter_types.go` | `DebugAdapterConfig`, `HandshakeDebugAdapterConfig`, adapter modes |
| `internal/dap/adapter_launcher.go` | `LaunchDebugAdapter()` — starts adapter processes in all 3 modes |
| `internal/dap/transport.go` | `Transport` interface with TCP, stdio, and Unix socket implementations |
| `internal/dap/process_runner.go` | `ProcessRunner` — launches processes for `runInTerminal` requests |
| `internal/exerunners/ide_executable_runner.go` | Integration point — registers bridge sessions, includes socket path in run requests |
| `internal/exerunners/ide_requests_responses.go` | Protocol types, API version definitions, `ideRunSessionRequestV1` with bridge fields |
| `internal/exerunners/ide_connection_info.go` | Version negotiation, `SupportsDebugBridge()` helper |
