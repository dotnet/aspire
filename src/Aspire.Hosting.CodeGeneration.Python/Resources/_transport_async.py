# _transport.py - ATS transport layer: RPC, Handle, errors, callbacks

from __future__ import annotations

import asyncio
import base64
from datetime import date, datetime, timedelta, time as dt_time, timezone
import json
import socket
import sys
from typing import Any, Callable, Awaitable, Protocol, TypedDict, Union, cast, runtime_checkable
from dataclasses import dataclass

# ============================================================================
# Base Types
# ============================================================================

MarshalledHandle = TypedDict('MarshalledHandle', {'$handle': str, '$type': str})
"""Represents a handle to a .NET object in the ATS system."""


class AtsErrorDetails(TypedDict, total=False):
    """Error details for ATS errors."""
    parameter: str
    expected: str
    actual: str


class AtsErrorData(TypedDict, total=False):
    """Structured error from ATS capability invocation."""
    code: str
    message: str
    capability: str
    details: AtsErrorDetails


class AtsErrorCodes:
    """ATS error codes returned by the server."""
    CAPABILITY_NOT_FOUND = "CAPABILITY_NOT_FOUND"
    HANDLE_NOT_FOUND = "HANDLE_NOT_FOUND"
    TYPE_MISMATCH = "TYPE_MISMATCH"
    INVALID_ARGUMENT = "INVALID_ARGUMENT"
    ARGUMENT_OUT_OF_RANGE = "ARGUMENT_OUT_OF_RANGE"
    CALLBACK_ERROR = "CALLBACK_ERROR"
    INTERNAL_ERROR = "INTERNAL_ERROR"


def is_ats_error(value: Any) -> bool:
    """Type guard to check if a value is an ATS error response."""
    return (
        isinstance(value, dict)
        and "$error" in value
        and isinstance(value["$error"], dict)
    )


def is_marshalled_handle(value: Any) -> bool:
    """Type guard to check if a value is a marshalled handle."""
    return (
        isinstance(value, dict)
        and "$handle" in value
        and "$type" in value
    )


# ============================================================================
# Handle
# ============================================================================

class Handle:
    """
    A typed handle to a .NET object in the ATS system.
    Handles are opaque references that can be passed to capabilities.
    """

    def __init__(self, marshalled: MarshalledHandle) -> None:
        self._handle_id = marshalled["$handle"]
        self._type_id = marshalled["$type"]

    @property
    def handle_id(self) -> str:
        """The handle ID (instance number)"""
        return self._handle_id

    @property
    def type_id(self) -> str:
        """The ATS type ID"""
        return self._type_id

    def to_json(self) -> MarshalledHandle:
        """Serialize for JSON-RPC transport"""
        return {
            "$handle": self._handle_id,
            "$type": self._type_id
        }

    def __str__(self) -> str:
        """String representation for debugging"""
        return f"Handle<{self._type_id}>({self._handle_id})"

    def __repr__(self) -> str:
        return self.__str__()


# ============================================================================
# Handle Wrapper Registry
# ============================================================================

HandleWrapperFactory = Callable[[Handle, "AspireClient"], Any]

# Registry of handle wrapper factories by type ID
_handle_wrapper_registry: dict[str, HandleWrapperFactory] = {}


def register_handle_wrapper(type_id: str, factory: HandleWrapperFactory) -> None:
    """
    Register a wrapper factory for a type ID.
    Called by generated code to register wrapper classes.
    """
    _handle_wrapper_registry[type_id] = factory


def wrap_if_handle(value: Any, client: "AspireClient | None" = None) -> Any:
    """
    Checks if a value is a marshalled handle and wraps it appropriately.
    Uses the wrapper registry to create typed wrapper instances when available.
    """
    if isinstance(value, dict) and is_marshalled_handle(value):
        handle = Handle(cast(MarshalledHandle, value))
        type_id = value["$type"]

        # Try to find a registered wrapper factory for this type
        if type_id and client:
            factory = _handle_wrapper_registry.get(type_id)
            if factory:
                return factory(handle, client)

        return handle

    return value


# ============================================================================
# Errors
# ============================================================================

class CapabilityError(Exception):
    """Error thrown when an ATS capability invocation fails."""

    def __init__(self, error: AtsErrorData) -> None:
        super().__init__(error.get("message", "Unknown error"))
        self.error = error

    @property
    def code(self) -> str:
        """Machine-readable error code"""
        return self.error.get("code", "UNKNOWN")

    @property
    def capability(self) -> str | None:
        """The capability that failed (if applicable)"""
        return self.error.get("capability")


# ============================================================================
# Callback Registry
# ============================================================================

CallbackFunction = Callable[..., Awaitable[Any]]

_callback_registry: dict[str, CallbackFunction] = {}
_callback_id_counter = 0


def register_callback(callback: CallbackFunction) -> str:
    """
    Register a callback function that can be invoked from the .NET side.
    Returns a callback ID that should be passed to methods accepting callbacks.

    .NET passes arguments as an object with positional keys: { p0: value0, p1: value1, ... }
    This function automatically extracts positional parameters and wraps handles.
    """
    global _callback_id_counter
    _callback_id_counter += 1
    callback_id = f"callback_{_callback_id_counter}_{id(callback)}"

    async def wrapper(args: Any, client: AspireClient) -> Any:
        # .NET sends args as object { p0: value0, p1: value1, ... }
        if isinstance(args, dict):
            arg_array = []
            i = 0
            while True:
                key = f"p{i}"
                if key in args:
                    arg_array.append(wrap_if_handle(args[key], client))
                    i += 1
                else:
                    break

            if arg_array:
                return await callback(*arg_array)

        # No args or null
        if args is None:
            return await callback()

        # Single primitive value (shouldn't happen with current protocol)
        return await callback(wrap_if_handle(args, client))

    _callback_registry[callback_id] = wrapper
    return callback_id


def unregister_callback(callback_id: str) -> bool:
    """Unregister a callback by its ID."""
    if callback_id in _callback_registry:
        del _callback_registry[callback_id]
        return True
    return False


def get_callback_count() -> int:
    """Get the number of registered callbacks."""
    return len(_callback_registry)


# ============================================================================
# JSON Encoder
# ============================================================================

@runtime_checkable
class ReferenceHandle(Protocol):
    """Protocol for objects that have a handle property."""

    @property
    def handle(self) -> Handle:
        ...


def _timedelta_as_isostr(td: timedelta) -> str:
    """Converts a datetime.timedelta object into an ISO 8601 formatted string, e.g. 'P4DT12H30M05S'

    Function adapted from the Tin Can Python project: https://github.com/RusticiSoftware/TinCanPython

    :param td: The timedelta object to convert
    :type td: datetime.timedelta
    :return: An ISO 8601 formatted string representing the timedelta object
    :rtype: str
    """

    # Split seconds to larger units
    seconds = td.total_seconds()
    minutes, seconds = divmod(seconds, 60)
    hours, minutes = divmod(minutes, 60)
    days, hours = divmod(hours, 24)

    days, hours, minutes = list(map(int, (days, hours, minutes)))
    seconds = round(seconds, 6)

    # Build date
    date_str = ""
    if days:
        date_str = "%sD" % days

    # Build time
    time_str = "T"

    # Hours
    bigger_exists = date_str or hours
    if bigger_exists:
        time_str += "{:02}H".format(hours)

    # Minutes
    bigger_exists = bigger_exists or minutes
    if bigger_exists:
        time_str += "{:02}M".format(minutes)

    # Seconds
    try:
        if seconds.is_integer():
            seconds_string = "{:02}".format(int(seconds))
        else:
            # 9 chars long w/ leading 0, 6 digits after decimal
            seconds_string = "%09.6f" % seconds
            # Remove trailing zeros
            seconds_string = seconds_string.rstrip("0")
    except AttributeError:  # int.is_integer() raises
        seconds_string = "{:02}".format(seconds)

    time_str += "{}S".format(seconds_string)
    return "P" + date_str + time_str


def _datetime_as_isostr(dt: Union[datetime, date, dt_time, timedelta]) -> str:
    """Converts a datetime.(datetime|date|time|timedelta) object into an ISO 8601 formatted string.

    :param dt: The datetime object to convert
    :type dt: datetime.datetime or datetime.date or datetime.time or datetime.timedelta
    :return: An ISO 8601 formatted string representing the datetime object
    :rtype: str
    """
    # First try datetime.datetime
    if hasattr(dt, "year") and hasattr(dt, "hour"):
        dt = cast(datetime, dt)
        # astimezone() fails for naive times in Python 2.7, so make make sure dt is aware (tzinfo is set)
        if not dt.tzinfo:
            iso_formatted = dt.replace(tzinfo=timezone.utc).isoformat()
        else:
            iso_formatted = dt.astimezone(timezone.utc).isoformat()
        # Replace the trailing "+00:00" UTC offset with "Z" (RFC 3339: https://www.ietf.org/rfc/rfc3339.txt)
        return iso_formatted.replace("+00:00", "Z")
    # Next try datetime.date or datetime.time
    try:
        dt = cast(Union[date, dt_time], dt)
        return dt.isoformat()
    # Last, try datetime.timedelta
    except AttributeError:
        dt = cast(timedelta, dt)
        return _timedelta_as_isostr(dt)


class AspireJSONEncoder(json.JSONEncoder):
    """A JSON encoder that's capable of serializing datetime objects and bytes."""

    def default(self, o: Any) -> Any:
        """Override the default method to handle datetime and bytes serialization.
        :param o: The object to serialize.
        :type o: Any
        :return: A JSON-serializable representation of the object.
        :rtype: Any
        """
        from ._base import ReferenceExpression

        if isinstance(o, ReferenceExpression):
            return o.to_json()
        if isinstance(o, ReferenceHandle):
            return o.handle.to_json()
        if isinstance(o, Handle):
            return o.to_json()
        if isinstance(o, (bytes, bytearray)):
            return base64.b64encode(o).decode()
        try:
            return _datetime_as_isostr(o)
        except AttributeError:
            pass
        return super().default(o)


# ============================================================================
# JSON-RPC Protocol
# ============================================================================

@dataclass
class JsonRpcRequest:
    """JSON-RPC 2.0 request"""
    jsonrpc: str = "2.0"
    id: int | None = None
    method: str = ""
    params: Any = None


@dataclass
class JsonRpcResponse:
    """JSON-RPC 2.0 response"""
    jsonrpc: str = "2.0"
    id: int | None = None
    result: Any = None
    error: dict[str, Any] | None = None


# ============================================================================
# AspireClient (JSON-RPC Connection)
# ============================================================================

class AspireClient:
    """Client for connecting to the Aspire AppHost via socket/named pipe."""

    def __init__(self, socket_path: str) -> None:
        self.socket_path = socket_path
        self._socket: socket.socket | None = None
        self._reader: asyncio.StreamReader | None = None
        self._writer: asyncio.StreamWriter | None = None
        self._request_id = 0
        self._pending_requests: dict[int, asyncio.Future[Any]] = {}
        self._disconnect_callbacks: list[Callable[[], None]] = []
        self._receive_task: asyncio.Task[None] | None = None
        self._connected = False

    def on_disconnect(self, callback: Callable[[], None]) -> None:
        """Register a callback to be called when the connection is lost"""
        self._disconnect_callbacks.append(callback)

    def _notify_disconnect(self) -> None:
        """Notify all disconnect callbacks"""
        for callback in self._disconnect_callbacks:
            try:
                callback()
            except Exception:
                pass

    async def connect(self, timeout_ms: int = 5000) -> None:
        """Connect to the Aspire AppHost"""
        try:
            timeout_sec = timeout_ms / 1000.0

            # On Windows, use named pipes; on Unix, use Unix domain sockets
            if sys.platform == "win32":
                pipe_path = f"\\\\.\\pipe\\{self.socket_path}"
                # On Windows, asyncio doesn't support named pipes directly,
                # so we use a socket connection approach
                reader, writer = await asyncio.wait_for(
                    asyncio.open_connection(host=None, port=None, path=pipe_path),
                    timeout=timeout_sec
                )
            else:
                reader, writer = await asyncio.wait_for(
                    asyncio.open_unix_connection(self.socket_path),
                    timeout=timeout_sec
                )

            self._reader = reader
            self._writer = writer
            self._connected = True

            # Start receiving messages
            self._receive_task = asyncio.create_task(self._receive_loop())

        except asyncio.TimeoutError:
            raise TimeoutError("Connection timeout")

    async def _receive_loop(self) -> None:
        """Receive and process messages from the server"""
        try:
            while self._connected and self._reader:
                # Read message length (4 bytes, big-endian)
                length_bytes = await self._reader.readexactly(4)
                message_length = int.from_bytes(length_bytes, byteorder="big")

                # Read message content
                message_bytes = await self._reader.readexactly(message_length)
                message_str = message_bytes.decode("utf-8")
                message = json.loads(message_str)

                # Handle response or request
                if "method" in message:
                    # This is a request from the server (callback invocation)
                    await self._handle_server_request(message)
                elif "id" in message:
                    # This is a response to our request
                    request_id = message["id"]
                    if request_id in self._pending_requests:
                        future = self._pending_requests.pop(request_id)
                        if "error" in message:
                            future.set_exception(Exception(message["error"]["message"]))
                        else:
                            future.set_result(message.get("result"))

        except asyncio.CancelledError:
            pass
        except Exception as e:
            print(f"Error in receive loop: {e}", file=sys.stderr)
        finally:
            self._connected = False
            self._notify_disconnect()

    async def _handle_server_request(self, message: dict[str, Any]) -> None:
        """Handle a request from the server (e.g., callback invocation)"""
        method = message.get("method")
        request_id = message.get("id")
        params = message.get("params", [])

        if method == "invokeCallback":
            callback_id = params[0] if len(params) > 0 else None
            args = params[1] if len(params) > 1 else None

            result = None
            error = None

            try:
                if callback_id and callback_id in _callback_registry:
                    callback = _callback_registry[callback_id]
                    result = await callback(args, self)
                else:
                    error = {"code": -32601, "message": f"Callback not found: {callback_id}"}
            except Exception as e:
                error = {"code": -32603, "message": str(e)}

            # Send response
            if request_id is not None:
                response = {
                    "jsonrpc": "2.0",
                    "id": request_id,
                    "result": result,
                    "error": error
                }
                await self._send_message(response)

    async def _send_message(self, message: dict[str, Any]) -> None:
        """Send a JSON-RPC message to the server"""
        if not self._writer:
            raise RuntimeError("Not connected")

        message_str = json.dumps(message)
        message_bytes = message_str.encode("utf-8")
        message_length = len(message_bytes)

        # Send length prefix (4 bytes, big-endian)
        length_bytes = message_length.to_bytes(4, byteorder="big")
        self._writer.write(length_bytes)
        self._writer.write(message_bytes)
        await self._writer.drain()

    async def ping(self) -> str:
        """Ping the server"""
        if not self._connected:
            raise RuntimeError("Not connected to AppHost")
        return await self._send_request("ping")

    async def invoke_capability(
        self,
        capability_id: str,
        args: dict[str, Any] | None = None
    ) -> Any:
        """
        Invoke an ATS capability by ID.

        Capabilities are operations exposed by [AspireExport] attributes.
        Results are automatically wrapped in Handle objects when applicable.
        """
        if not self._connected:
            raise RuntimeError("Not connected to AppHost")

        result = await self._send_request("invokeCapability", capability_id, args or {})

        # Check for structured error response
        if is_ats_error(result):
            raise CapabilityError(result["$error"])

        # Wrap handles automatically
        return wrap_if_handle(result, self)

    async def _send_request(self, method: str, *params: Any) -> Any:
        """Send a JSON-RPC request and wait for response"""
        self._request_id += 1
        request_id = self._request_id

        request = {
            "jsonrpc": "2.0",
            "id": request_id,
            "method": method,
            "params": list(params) if params else []
        }

        # Create future for response
        future: asyncio.Future[Any] = asyncio.Future()
        self._pending_requests[request_id] = future

        # Send request
        await self._send_message(request)

        # Wait for response
        return await future

    async def disconnect(self) -> None:
        """Disconnect from the server"""
        self._connected = False

        if self._receive_task:
            self._receive_task.cancel()
            try:
                await self._receive_task
            except asyncio.CancelledError:
                pass

        if self._writer:
            self._writer.close()
            await self._writer.wait_closed()

        self._reader = None
        self._writer = None

    @property
    def connected(self) -> bool:
        """Check if connected to the server"""
        return self._connected
