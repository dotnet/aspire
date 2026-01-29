# _transport.py - ATS transport layer: RPC, Handle, errors, callbacks (sync/threaded)

from __future__ import annotations

import base64
from datetime import date, datetime, timedelta, time as dt_time, timezone
import json
import logging
import signal
import socket
import sys
import threading
import time
import traceback
from typing import Any, Callable, Mapping, Protocol, TypedDict, Union, cast, runtime_checkable, TYPE_CHECKING

_logger = logging.getLogger("aspyre")

# ============================================================================
# Platform-specific Socket Implementation
# ============================================================================

if sys.platform == "win32":
    import ctypes
    from ctypes import wintypes

    _kernel32 = ctypes.WinDLL('kernel32', use_last_error=True)

    class _OVERLAPPED(ctypes.Structure):
        """Windows OVERLAPPED structure for async I/O."""
        _fields_ = [
            ("Internal", ctypes.POINTER(ctypes.c_ulong)),
            ("InternalHigh", ctypes.POINTER(ctypes.c_ulong)),
            ("Offset", wintypes.DWORD),
            ("OffsetHigh", wintypes.DWORD),
            ("hEvent", wintypes.HANDLE),
        ]

    class _PipeSocket:
        """A socket-like wrapper around a Win32 named pipe using ctypes."""

        # Win32 constants
        GENERIC_READ = 0x80000000
        GENERIC_WRITE = 0x40000000
        OPEN_EXISTING = 3
        INVALID_HANDLE_VALUE = -1
        FILE_FLAG_OVERLAPPED = 0x40000000
        ERROR_IO_PENDING = 997
        ERROR_FILE_NOT_FOUND = 2

        def __init__(self, pipe_path: str) -> None:
            self._handle: int | None = None
            handle = _kernel32.CreateFileW(
                pipe_path,
                self.GENERIC_READ | self.GENERIC_WRITE,
                0,  # no sharing
                None,  # default security
                self.OPEN_EXISTING,
                self.FILE_FLAG_OVERLAPPED,  # match server's async mode
                None  # no template
            )

            if handle == self.INVALID_HANDLE_VALUE:
                error = ctypes.get_last_error()
                if error == self.ERROR_FILE_NOT_FOUND:
                    raise FileNotFoundError(f"Pipe not found: {pipe_path}")
                raise OSError(f"CreateFile failed with error {error}")

            self._handle = handle

        def _create_overlapped_event(self) -> _OVERLAPPED:
            """Create an OVERLAPPED structure with an event for async I/O."""
            overlapped = _OVERLAPPED()
            overlapped.hEvent = _kernel32.CreateEventW(None, True, False, None)
            return overlapped

        def recv(self, n: int) -> bytes:
            """Read up to n bytes using overlapped I/O."""
            buffer = ctypes.create_string_buffer(n)
            bytes_read = wintypes.DWORD()
            overlapped = self._create_overlapped_event()

            try:
                success = _kernel32.ReadFile(
                    self._handle,
                    buffer,
                    n,
                    ctypes.byref(bytes_read),
                    ctypes.byref(overlapped)
                )

                if not success:
                    error = ctypes.get_last_error()
                    if error == self.ERROR_IO_PENDING:
                        # Wait for the operation to complete
                        _kernel32.GetOverlappedResult(
                            self._handle,
                            ctypes.byref(overlapped),
                            ctypes.byref(bytes_read),
                            True  # wait
                        )
                    else:
                        raise OSError(f"ReadFile failed with error {error}")
            finally:
                _kernel32.CloseHandle(overlapped.hEvent)

            return buffer.raw[:bytes_read.value]

        def sendall(self, data: bytes) -> None:
            """Write all data using overlapped I/O."""
            bytes_written = wintypes.DWORD()
            overlapped = self._create_overlapped_event()

            try:
                offset = 0
                while offset < len(data):
                    chunk = data[offset:]

                    success = _kernel32.WriteFile(
                        self._handle,
                        chunk,
                        len(chunk),
                        ctypes.byref(bytes_written),
                        ctypes.byref(overlapped)
                    )

                    if not success:
                        error = ctypes.get_last_error()
                        if error == self.ERROR_IO_PENDING:
                            # Wait for the operation to complete
                            _kernel32.GetOverlappedResult(
                                self._handle,
                                ctypes.byref(overlapped),
                                ctypes.byref(bytes_written),
                                True  # wait
                            )
                        else:
                            raise OSError(f"WriteFile failed with error {error}")

                    offset += bytes_written.value
            finally:
                _kernel32.CloseHandle(overlapped.hEvent)

        def close(self) -> None:
            """Close the handle."""
            if self._handle is not None and self._handle != self.INVALID_HANDLE_VALUE:
                _kernel32.CloseHandle(self._handle)
                self._handle = None

    def _connect_pipe(socket_path: str, timeout_sec: float) -> _PipeSocket:
        """Connect to a named pipe with timeout, retrying until available."""
        pipe_path = f"\\\\.\\pipe\\{socket_path}"
        _logger.debug("Connecting to: %s", pipe_path)

        start_time = time.time()
        while (time.time() - start_time) < timeout_sec:
            try:
                return _PipeSocket(pipe_path)
            except FileNotFoundError:
                time.sleep(0.1)
        raise TimeoutError("Connection timeout")

else:
    # On Unix, use socket.socket directly as the pipe socket type
    _PipeSocket = socket.socket  # type: ignore[misc]

    def _default_unix_socket_path(name: str) -> str:
        """Get the default Unix socket path for a given name."""
        import os
        runtime_dir = os.environ.get("XDG_RUNTIME_DIR")
        if runtime_dir and os.path.isdir(runtime_dir):
            return os.path.join(runtime_dir, f"{name}.sock")
        return f"/tmp/{name}.sock"

    def _connect_pipe(socket_path: str, timeout_sec: float) -> _PipeSocket:
        """Connect to a Unix domain socket with timeout."""
        # Format socket path if it's just a name (no path separators)
        if "/" not in socket_path:
            socket_path = _default_unix_socket_path(socket_path)

        _logger.debug("Connecting to: %s", socket_path)

        start_time = time.time()
        while (time.time() - start_time) < timeout_sec:
            try:
                sock = _PipeSocket(socket.AF_UNIX, socket.SOCK_STREAM)
                sock.settimeout(timeout_sec)
                sock.connect(socket_path)
                sock.settimeout(None)  # Set to blocking mode
                return sock
            except FileNotFoundError:
                time.sleep(0.1)
        raise TimeoutError("Connection timeout")

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

    def __repr__(self) -> str:
        """String representation for debugging"""
        friendly_name = self._type_id.split("/")[-1].split(".")[-1]
        return f"Handle[{friendly_name}]({self._handle_id})"


# ============================================================================
# Handle Wrapper Registry
# ============================================================================

HandleWrapperFactory = Callable[[Handle, "AspireClient"], Any]

# Registry of handle wrapper factories by type ID
_handle_wrapper_registry: dict[str, HandleWrapperFactory] = {}


def _register_handle_wrapper(type_id: str, factory: HandleWrapperFactory) -> None:
    """
    Register a wrapper factory for a type ID.
    Called by generated code to register wrapper classes.
    """
    _handle_wrapper_registry[type_id] = factory


def wrap_if_handle(value: Any, client: AspireClient | None = None, kwargs: Mapping[str, Any] | None = None) -> Any:
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
                if kwargs:
                    return factory(handle, client, **kwargs)
                return factory(handle, client)

        return handle

    return value


# ============================================================================
# Errors
# ============================================================================

class AspyreError(Exception):
    """Base class for Aspire errors."""

    def __init__(self, error: AtsErrorData) -> None:
        super().__init__(error.get("message", "Unknown error"))
        self.error = error

    @property
    def code(self) -> str:
        """Machine-readable error code"""
        return self.error.get("code", "UNKNOWN")


class CapabilityError(AspyreError):
    """Error thrown when an ATS capability invocation fails."""

    @property
    def capability(self) -> str:
        """The capability that failed (if applicable)"""
        return cast(str, self.error.get("capability"))


class ParameterTypeError(CapabilityError, TypeError):
    """Error thrown when a capability parameter is invalid."""
    pass


class CallbackCancelled(Exception):
    """Error thrown when a callback invocation is cancelled."""
    pass

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


class AspireClient:
    """Client for connecting to the Aspire AppHost via socket/named pipe (synchronous with threads)."""

    # Default heartbeat interval in seconds
    DEFAULT_HEARTBEAT_INTERVAL = 5.0

    def __init__(self, socket_path: str, *, debug: bool | None = None, heartbeat_interval: float | None = None) -> None:
        self.socket_path = socket_path
        self.debug = debug if debug is not None else False
        self._socket: _PipeSocket | None = None
        self._request_id = 0
        self._pending_requests: dict[int, threading.Event] = {}
        self._pending_results: dict[int, tuple[Any, Exception | None]] = {}
        self._disconnect_callbacks: list[Callable[[], None]] = []
        self._receive_thread: threading.Thread | None = None
        self._heartbeat_thread: threading.Thread | None = None
        self._cancellation_threads: dict[str, tuple[Callable[[], None], threading.Thread]] = {}
        self._cancellation_id = 0
        self._heartbeat_interval = heartbeat_interval if heartbeat_interval is not None else self.DEFAULT_HEARTBEAT_INTERVAL
        self._heartbeat_stop_event = threading.Event()
        self._connected = False
        self._connection_error: ConnectionError | None = None
        self._lock = threading.Lock()
        self._callback_registry: dict[str, Callable[..., Any]] = {}
        self._callback_id_counter = 0
        self._callback_threads: dict[str, threading.Thread] = {}

    def on_disconnect(self, callback: Callable[[], None]) -> None:
        """Register a callback to be called when the connection is lost"""
        with self._lock:
            self._disconnect_callbacks.append(callback)

    def _handle_sigint(self, signum: int, frame: Any) -> None:
        """Handle SIGINT (Ctrl+C) by cancelling all pending requests and closing connection."""
        # Close the connection
        self._close_connection(ConnectionError("Shutdown by user"))
        # TODO: This will just shutdown the process, which may be best as this will likely
        # be run with aspire.run, which will prefer a quick exit with zero exit code.
        # Not entirely sure if this signal will be propagated correctly by .NET.... well see.
        sys.exit(0)

    def _close_connection(self, error: ConnectionError | None = None) -> None:
        """
        Central method for closing the connection. Thread-safe and idempotent.

        Args:
            error: If provided, this is an unexpected disconnection and callbacks
                   will be notified. If None, this is an intentional disconnect.

        This method:
        - Closes the socket
        - Sets _connected = False
        - Stores the error (if provided and not already set)
        - Signals the heartbeat thread to stop
        - Notifies disconnect callbacks (only on first unexpected disconnection)
        """
        should_notify = False
        callbacks: list[Callable[[], None]] = []

        with self._lock:
            # Close socket if open
            if self._socket is not None:
                try:
                    self._socket.close()
                except Exception:
                    pass
                self._socket = None

            # Mark as disconnected
            should_notify = self._connected
            self._connected = False
            callbacks = list(self._disconnect_callbacks)
            if should_notify:
                if error:
                    _logger.info("Closing connection to AppHost with error: %s", error)
                else:
                    _logger.info("Closing connection to AppHost")

            # Handle error state
            if error is not None:
                # Unexpected disconnection - store error if first failure
                if self._connection_error is None:
                    self._connection_error = error
            else:
                # Intentional disconnect - clear any previous error?
                self._connection_error = None

            for event in self._pending_requests.values():
                event.set()
            for (cancellation, _ ) in self._cancellation_threads.values():
                # Threads will exit on their own when they notice disconnection
                cancellation()

        # Signal heartbeat to stop (outside lock, it's thread-safe)
        self._heartbeat_stop_event.set()

        # Notify callbacks outside lock to avoid deadlocks
        if should_notify:
            for callback in callbacks:
                try:
                    callback()
                except Exception:
                    pass

    def connect(self, timeout_ms: int = 10000) -> None:
        """Connect to the Aspire AppHost"""
        timeout_sec = timeout_ms / 1000.0
        socket = _connect_pipe(self.socket_path, timeout_sec)

        with self._lock:
            self._socket = socket
            self._connected = True
            self._connection_error = None

        self._heartbeat_stop_event.clear()

        # Install SIGINT handler for clean Ctrl+C handling
        signal.signal(signal.SIGINT, self._handle_sigint)

        _logger.info("Connected to AppHost")

        # Start receiving messages in a background thread
        self._receive_thread = threading.Thread(target=self._receive_loop, daemon=True)
        self._receive_thread.start()

        # Start heartbeat thread to monitor connection health
        self._heartbeat_thread = threading.Thread(target=self._heartbeat_loop, daemon=True)
        self._heartbeat_thread.start()

    def _recv_exactly(self, n: int) -> bytes:
        """Read exactly n bytes from the socket"""
        data = b""
        while len(data) < n:
            chunk = cast(_PipeSocket, self._socket).recv(n - len(data))
            if not chunk:
                raise ConnectionError("Connection closed")
            data += chunk
        return data

    def _read_line(self) -> bytes:
        """Read a line ending with \\r\\n from the socket"""
        line = b""
        while True:
            byte = cast(_PipeSocket, self._socket).recv(1)
            if not byte:
                raise ConnectionError("Connection closed")
            line += byte
            if line.endswith(b"\r\n"):
                return line[:-2]  # Remove \r\n

    def _read_headers(self) -> dict[str, str]:
        """Read HTTP-style headers until empty line"""
        headers: dict[str, str] = {}
        while True:
            line = self._read_line()
            if not line:  # Empty line signals end of headers
                break
            # Parse "Header-Name: value"
            if b":" in line:
                name, value = line.split(b":", 1)
                headers[name.decode("utf-8").strip().lower()] = value.decode("utf-8").strip()
        return headers

    def _receive_loop(self) -> None:
        """Receive and process messages from the server"""
        try:
            while self._connected and self._socket:
                # Read HTTP-style headers (HeaderDelimitedMessageHandler format)
                headers = self._read_headers()
                content_length = int(headers.get("content-length", "0"))

                if content_length == 0:
                    continue

                # Read message content
                message_bytes = self._recv_exactly(content_length)
                message_str = message_bytes.decode("utf-8")
                message = json.loads(message_str)
                if self.debug:
                    if message.get("result") == "pong":
                        _logger.debug("<- %s", message)
                    else:
                        _logger.info("<- %s", message)

                # Handle response or request
                if "method" in message:
                    # This is a request from the server (callback invocation)
                    self._handle_server_request(message)
                elif "id" in message:
                    # This is a response to our request
                    request_id = message["id"]
                    with self._lock:
                        if request_id in self._pending_requests:
                            event = self._pending_requests[request_id]
                            if "error" in message:
                                self._pending_results[request_id] = (None, Exception(message["error"]["message"]))
                            else:
                                self._pending_results[request_id] = (message.get("result"), None)
                            event.set()
        except AttributeError:
            # This probably means the socket was closed
            pass
        except ConnectionError as e:
            self._close_connection(e)
        except Exception as e:
            _logger.error("Error in receive loop: %s", e)
            self._close_connection(ConnectionError(f"Receive loop error: {e}"))

    def _heartbeat_loop(self) -> None:
        """Periodically ping the server to check connection health."""
        while not self._heartbeat_stop_event.wait(timeout=self._heartbeat_interval):
            with self._lock:
                if not self._connected:
                    break
            try:
                self.ping()
            except Exception as e:
                self._close_connection(ConnectionError(f"Heartbeat failed: {e}"))
                break

    def _handle_server_request(self, message: dict[str, Any]) -> None:
        """Handle a request from the server (e.g., callback invocation)"""
        method = message.get("method")
        request_id = message.get("id")
        params = message.get("params", [])

        if method == "invokeCallback":
            _logger.debug("Invoking callback with params: %s", params)
            callback_id = str(params[0]) if len(params) > 0 else None
            args = params[1] if len(params) > 1 else None

            # Spawn a separate thread to handle the callback so receive_loop isn't blocked
            thread_id = f"cb_thread_{request_id}_{callback_id}"
            thread = threading.Thread(
                target=self._execute_callback_thread,
                args=(callback_id, args, request_id, thread_id),
                daemon=True
            )
            with self._lock:
                self._callback_threads[thread_id] = thread
            thread.start()

    def _execute_callback_thread(
        self,
        callback_id: str | None,
        args: Any,
        request_id: int | None,
        thread_id: str
    ) -> None:
        """Execute a callback in a separate thread and send the response."""
        result = None
        error = None
        try:
            try:
                with self._lock:
                    callback = self._callback_registry.get(callback_id) if callback_id else None

                if callback:
                    result = callback(args, self)
                    _logger.debug("Callback result: %s", result)
                else:
                    error = {"code": -32601, "message": f"Callback not found: {callback_id}"}
            except CallbackCancelled as e:
                try:
                    cancellation_token = e.args[0]
                    _logger.info("Callback cancelled: %s", e, cancellation_token)
                    self._send_request("cancelToken", cancellation_token)
                except Exception:
                    pass  # Ignore errors during cancellation
                return

            except Exception as e:
                _logger.warning("Exception in callback: %s", e)
                error = {"code": -32603, "message": f"{e}\n{traceback.format_exc()}"}

            # Send response
            if request_id is not None:
                if error:
                    response = {
                        "jsonrpc": "2.0",
                        "id": request_id,
                        "error": error
                    }
                else:
                    response = {
                        "jsonrpc": "2.0",
                        "id": request_id,
                        "result": result
                    }
                try:
                    self._send_message(response)
                except Exception as e:
                    _logger.error("Failed to send callback response: %s", e)
        finally:
            # Clean up thread reference
            with self._lock:
                self._callback_threads.pop(thread_id, None)

    def _send_message(self, message: dict[str, Any]) -> None:
        """Send a JSON-RPC message to the server using header-delimited format"""
        if self.debug:
            if message.get("method") == "ping":
                _logger.debug("-> %s", message)
            else:
                _logger.info("-> %s", message)
        message_str = json.dumps(message, cls=AspireJSONEncoder)
        message_bytes = message_str.encode("utf-8")
        content_length = len(message_bytes)

        # Send with HTTP-style headers (HeaderDelimitedMessageHandler format)
        header = f"Content-Length: {content_length}\r\n\r\n"
        header_bytes = header.encode("utf-8")
        with self._lock:
            cast(_PipeSocket, self._socket).sendall(header_bytes + message_bytes)

    def _check_connection(self) -> None:
        """Check if connected and raise stored connection error if present."""
        with self._lock:
            if self._connection_error:
                raise self._connection_error
            if not self._connected:
                raise RuntimeError("Not connected to AppHost")

    def ping(self) -> str:
        """Ping the server"""
        self._check_connection()
        return self._send_request("ping")

    def invoke_capability(
        self,
        capability_id: str,
        args: dict[str, Any] | None = None,
        kwargs: Mapping[str, Any] | None = None
    ) -> Any:
        """
        Invoke an ATS capability by ID.

        Capabilities are operations exposed by [AspireExport] attributes.
        Results are automatically wrapped in Handle objects when applicable.
        """
        self._check_connection()
        result = self._send_request("invokeCapability", capability_id, args or {})

        # Check for structured error response
        if is_ats_error(result):
            raise CapabilityError(result["$error"])

        # Wrap handles automatically
        return wrap_if_handle(result, self, kwargs)

    def _send_request(self, method: str, *params: Any) -> Any:
        """Send a JSON-RPC request and wait for response"""
        with self._lock:
            self._request_id += 1
            request_id = self._request_id

        request = {
            "jsonrpc": "2.0",
            "id": request_id,
            "method": method,
            "params": list(params) if params else []
        }
        # Create event for response
        event = threading.Event()
        with self._lock:
            self._pending_requests[request_id] = event

        # Send request
        self._send_message(request)

        # Wait for response
        event.wait()

        # Get result
        with self._lock:
            if request_id not in self._pending_results:
                # Request was cancelled/interrupted
                if self._connection_error:
                    raise self._connection_error
                raise RuntimeError("Request was cancelled")
            del self._pending_requests[request_id]
            result, error = self._pending_results.pop(request_id)

        if error:
            raise error

        return result

    def register_cancellation_token(self, cancellation_timeout: int | None) -> str | None:
        if not cancellation_timeout:
            return None

        with self._lock:
            cancellation_id = f"ct_{self._cancellation_id}_{int(time.time() * 1000)}"
            cancellation_token = threading.Event()

            def cancellation_thread():
                cancellation_token.wait()
                self._check_connection()
                # Send cancellation request to server
                try:
                    self._send_request("cancelToken", cancellation_id)
                except Exception:
                    pass  # Ignore errors during cancellation
                self._cancellation_threads.pop(cancellation_id, None)

            thread = threading.Thread(target=cancellation_thread, daemon=True)
            cancel_timer = threading.Timer(cancellation_timeout, cancellation_token.set)

            def shutdown():
                cancel_timer.cancel()
                cancellation_token.set()

            self._cancellation_threads[cancellation_id] = (shutdown, thread)
            self._cancellation_id += 1
            thread.start()
            cancel_timer.start()

        return cancellation_id

    def disconnect(self) -> None:
        """Disconnect from the server"""
        self._close_connection(error=None)  # Intentional disconnect, no error

        if self._heartbeat_thread and self._heartbeat_thread.is_alive():
            self._heartbeat_thread.join(timeout=1.0)

        if self._receive_thread and self._receive_thread.is_alive():
            self._receive_thread.join(timeout=1.0)

        # Wait for any pending callback threads to finish
        with self._lock:
            callback_threads = list(self._callback_threads.values())
        for thread in callback_threads:
            if thread.is_alive():
                thread.join(timeout=1.0)
        for (_, thread) in self._cancellation_threads.values():
            if thread.is_alive():
                thread.join(timeout=1.0)

    def register_callback(self, callback: Callable[..., Any] | None) -> str | None:
        """
        Register a callback function that can be invoked from the .NET side.
        Returns a callback ID that should be passed to methods accepting callbacks.

        .NET passes arguments as an object with positional keys: { p0: value0, p1: value1, ... }
        This function automatically extracts positional parameters and wraps handles.
        """
        if callback is None:
            return None

        with self._lock:
            self._callback_id_counter += 1
            callback_id = f"callback_{self._callback_id_counter}_{id(callback)}"

        def wrapper(args: Any, client: AspireClient) -> Any:
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
                    return callback(*arg_array)

            # No args or null
            if args is None:
                return callback()

            # Single primitive value (shouldn't happen with current protocol)
            return callback(wrap_if_handle(args, client))

        with self._lock:
            self._callback_registry[callback_id] = wrapper
        return callback_id

    @property
    def connected(self) -> bool:
        """Check if connected to the server"""
        return self._connected

