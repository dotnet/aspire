# Running .NET Aspire applications inside an integrated developer environment (IDE)

## Application host, DCP, and IDE
When .NET Aspire application host program is run, it does not launch application service programs or supporting emulators/containers directly. Instead, the application host relies on another program ("app orchestrator") called `DCP`.

When Visual Studio (or another IDE) starts the Aspire application host, Aspire-specific application model is created from user code in app host project. This model is then converted to another, language-agnostic model that DCP understands, and is submitted by app host to DCP for execution. The models are quite similar: for example, Aspire project (application services) are modeled as `Executable` objects on DCP side; containers become `Container` objects in DCP world.

By default, `Executable` objects that are part of DCP workload are run as ordinary operating system processes. That means, DCP uses appropriate OS call to start a child process that executes the program specified by `Executable` spec. For debugging application services this is often not convenient or useful: it is difficult to debug service startup code this way, or restart the process automatically when source code changes. This is why DCP supports an alternative method of running `Executable` object, which is called **IDE execution**.

With IDE execution DCP delegates the task of running the program represented by `Executable` object to an external entity. The entity can be an IDE such as Visual Studio or Visual Studio Code, or a CLI tool like `dotnet watch`, but in the rest of this document we will refer to it as **IDE**.

When IDE execution is enabled for an `Executable` object, DCP will go through normal execution preparation steps (such as computing the values of environment variables for the `Executable`), but instead of creating an OS process, DCP will issue a request to the IDE to run the `Executable` program, passing all relevant information in the request. DCP then relies on the IDE to start the program, creating a **run session**. The IDE is expected to inform DCP about changes to the session (e.g. program finished execution) and DCP reflects these changes in the workload model.

## Enabling IDE execution

For IDE execution to work, two conditions need to be fulfilled:

1. DCP needs to be told how to contact the IDE (what is the **IDE session endpoint**, specifically).
1. The `ExecutionType` property for the `Executable` object needs to be set to `IDE` (default is `Process`, which indicates OS process-based execution).

Only one IDE (one IDE session endpoint) is supported per DCP instance. The IDE session endpoint is provided to DCP via environment variables (both required if IDE execution is used):

| Environment variable | Value |
| ----- | ----- |
| `DEBUG_SESSION_PORT` | The port DCP should use to talk to the IDE session endpoint. DCP will use `http://localhost:<value of DEBUG_SESSION_PORT>` as the IDE session endpoint base URL. |
| `DEBUG_SESSION_TOKEN` | Security (bearer) token for talking to the IDE session endpoint. This token will be attached to every request via Authorization HTTP header. |

> Note: the most important use case for the IDE execution is to facilitate application services debugging. The word "debug" appears in environment variable names that DCP uses to connect to IDE session endpoint, but IDE execution does not always mean that the service is running under a debugger.

### Using multiple execution types in the same workload

It is possible to have a workload that uses both IDE execution for some `Executable` objects, and process execution for other `Executable` objects. Every `Executable` object is treated separately according to the `ExecutionType` spec property.

If multiple replicas are used (created via `ExecutableReplicaSet` objects), all replicas belonging to the same replica set will use the same execution type.

## IDE session endpoint requests

The IDE session endpoint is expected to support the following requests, delivered via HTTP protocol, and using JSON as the payload format:

### Create session request

Used to create a new run session for particular `Executable`.

**HTTP verb and path** <br/>
`PUT /run_session`

**Headers** <br/>
`Authorization: Bearer <security token>` <br/>
`Content-Type: application/json`

**Payload** <br/>

The payload is best explained using an example:

```jsonc
{
    "project_path": "<path to C# project file for the program>",
    "debug": true, // Whether the program should be running under the debugger
    "env" : [
        // Environment variables are modeled as objects, with 'name' and 'value' property, for example:
        { "name": "NO_COLOR", "value": "1" },
        { "name": "EMPTY_VALUE_VAR", "value": "" }
    ],
    "args": [
        // Invocation arguments for the program (modeled as array of strings)
        "-v",
        "1"
    ],
    "launch_profile": "<name of the launch profile to use for the program, optional>"
}
```

**Response** <br/>
If the execution session is created successfully, the return status code should be 200 OK or 201 Created. The response should include the created run session identifier in the `Location` header:

`Location: http://localhost:<IDE endpoint port>/run_session/<new run ID>`

If the session cannot be created, appropriate 4xx or 5xx status code should be returned. The response might also return a description of the problem as part of the status line, or in the response body.

### Stop session request

Used to stop an in-progress run session

**HTTP verb and path** <br/>
`DELETE /run_session/<run session ID>`

**Headers** <br/>
`Authorization: Bearer <security token>`

**Response** <br/>
If the session exists and can be stopped, the IDE should reply with 200 OK status code.

If the session does not exist, the IDE should reply with 204 No Content.

If the session cannot be stopped, appropriate 4xx or 5xx status code should be returned. The response might also return a description of the problem as part of the status line, or in the response body.

### Subscribe to session change notifications request

Used by DCP to subscribe to run session change notification.

**HTTP verb and path** <br/>
`GET /run_session/notify`

**Headers** <br/>
`Authorization: Bearer <security token>` <br/>
(+ web sockets connection upgrade headers)

**Response** <br/>
If successful, the connection should be upgraded to a web sockets connection, which will be then used by the IDE to stream run session change notifications to DCP. See next paragraph for description of possible change notifications.

## Run session change notifications

The run session change notifications are delivered from IDE to DCP via the web socket connection. The format of notification is JSON Lines (one JSON object per line of text).

All run sessions share the same notification stream. Notifications are delivered in near-real-time as sessions change--there is no memory/reply of notifications for sessions that occurred in the past. DCP subscribes to run session change notifications before it makes the first request to create a run session; this ensures that it will receive all change notifications for all sessions it creates.

### Common notification properties

Every run change notification has the following properties:

| Property | Description | Type |
| --- | --------- | --- |
| `notification_type` | One of `processRestarted`, `sessionTerminated`, or `serviceLogs`: indicates the type of notification. | `string` (limited set of values) |
| `session_id` | The ID of the run session that the notification is related to. | `string` |

### Process restarted notification

The process (re)started notification in emitted when the run is started, and whenever the IDE restarts the service (upon developer request). Properties specific to this notification are:

| Property | Description | Type |
| --- | --------- | --- |
| `notification_type` | Must be `processRestarted` | `string` |
| `pid` | The process ID of the service process associated with the run session. | `number` (representing unsigned 32-bit integer) |

### Session terminated notification

Session terminated notification is emitted when the session is terminated (the program ends, or is terminated by the developer). Properties specific to this notification are:

| Property | Description | Type |
| --- | --------- | --- |
| `notification_type` | Must be `sessionTerminated` | `string` |
| `exit_code` | The exit code of the process associated with the run session. | `number` (representing unsigned 32-bit integer) |

### Log notification

The log notification is emitted when the service program writes something to standard output stream (`stdout`) or standard error (`stderr`). Properties specific to this notification are:

| Property | Description | Type |
| --- | --------- | --- |
| `notification_type` | Must be `serviceLogs` | `string` |
| `is_std_err` | True if the output comes from standard error stream, otherwise false (implying standard output stream). | `boolean` |
| `log_message` | The text written by the service program. | `string` |
