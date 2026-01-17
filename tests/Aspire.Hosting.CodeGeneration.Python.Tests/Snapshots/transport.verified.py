# transport.py - ATS transport layer: JSON-RPC, Handle, callbacks, cancellation
from __future__ import annotations

import asyncio
import json
import os
import socket
import threading
import time
from typing import Any, Callable, Dict, Optional


class AtsErrorCodes:
    CapabilityNotFound = "CAPABILITY_NOT_FOUND"
    HandleNotFound = "HANDLE_NOT_FOUND"
    TypeMismatch = "TYPE_MISMATCH"
    InvalidArgument = "INVALID_ARGUMENT"
    ArgumentOutOfRange = "ARGUMENT_OUT_OF_RANGE"
    CallbackError = "CALLBACK_ERROR"
    InternalError = "INTERNAL_ERROR"


class CapabilityError(RuntimeError):
    def __init__(self, error: Dict[str, Any]) -> None:
        super().__init__(error.get("message", "Capability error"))
        self.error = error

    @property
    def code(self) -> str:
        return self.error.get("code", "")

    @property
    def capability(self) -> Optional[str]:
        return self.error.get("capability")


class Handle:
    def __init__(self, marshalled: Dict[str, str]) -> None:
        self._handle_id = marshalled["$handle"]
        self._type_id = marshalled["$type"]

    @property
    def handle_id(self) -> str:
        return self._handle_id

    @property
    def type_id(self) -> str:
        return self._type_id

    def to_json(self) -> Dict[str, str]:
        return {"$handle": self._handle_id, "$type": self._type_id}

    def __str__(self) -> str:
        return f"Handle<{self._type_id}>({self._handle_id})"


def is_marshalled_handle(value: Any) -> bool:
    return isinstance(value, dict) and "$handle" in value and "$type" in value


def is_ats_error(value: Any) -> bool:
    return isinstance(value, dict) and "$error" in value


_handle_wrapper_registry: Dict[str, Callable[[Handle, "AspireClient"], Any]] = {}
_callback_registry: Dict[str, Callable[..., Any]] = {}
_callback_lock = threading.Lock()
_callback_counter = 0


def register_handle_wrapper(type_id: str, factory: Callable[[Handle, "AspireClient"], Any]) -> None:
    _handle_wrapper_registry[type_id] = factory


def wrap_if_handle(value: Any, client: Optional["AspireClient"] = None) -> Any:
    if is_marshalled_handle(value):
        handle = Handle(value)
        if client is not None:
            factory = _handle_wrapper_registry.get(handle.type_id)
            if factory:
                return factory(handle, client)
        return handle
    return value


def register_callback(callback: Callable[..., Any]) -> str:
    global _callback_counter
    with _callback_lock:
        _callback_counter += 1
        callback_id = f"callback_{_callback_counter}_{int(time.time() * 1000)}"
    _callback_registry[callback_id] = callback
    return callback_id


def unregister_callback(callback_id: str) -> bool:
    return _callback_registry.pop(callback_id, None) is not None


class CancellationToken:
    def __init__(self) -> None:
        self._callbacks: list[Callable[[], None]] = []
        self._cancelled = False

    def cancel(self) -> None:
        if self._cancelled:
            return
        self._cancelled = True
        for callback in list(self._callbacks):
            callback()
        self._callbacks.clear()

    def register(self, callback: Callable[[], None]) -> Callable[[], None]:
        if self._cancelled:
            callback()
            return lambda: None
        self._callbacks.append(callback)

        def unregister() -> None:
            if callback in self._callbacks:
                self._callbacks.remove(callback)

        return unregister


def register_cancellation(token: Optional[CancellationToken], client: "AspireClient") -> Optional[str]:
    if token is None:
        return None
    cancellation_id = f"ct_{int(time.time() * 1000)}_{id(token)}"
    token.register(lambda: client.cancel_token(cancellation_id))
    return cancellation_id


class AspireClient:
    def __init__(self, socket_path: str) -> None:
        self._socket_path = socket_path
        self._stream: Optional[Any] = None
        self._next_id = 1
        self._disconnect_callbacks: list[Callable[[], None]] = []
        self._connected = False
        self._io_lock = threading.Lock()

    def connect(self) -> None:
        if self._connected:
            return
        self._stream = _open_stream(self._socket_path)
        self._connected = True

    def on_disconnect(self, callback: Callable[[], None]) -> None:
        self._disconnect_callbacks.append(callback)

    def invoke_capability(self, capability_id: str, args: Optional[Dict[str, Any]] = None) -> Any:
        result = self._send_request("invokeCapability", [capability_id, args])
        if is_ats_error(result):
            raise CapabilityError(result["$error"])
        return wrap_if_handle(result, self)

    def cancel_token(self, token_id: str) -> bool:
        return bool(self._send_request("cancelToken", [token_id]))

    def disconnect(self) -> None:
        self._connected = False
        if self._stream:
            try:
                self._stream.close()
            finally:
                self._stream = None
        for callback in self._disconnect_callbacks:
            try:
                callback()
            except Exception:
                pass

    def _send_request(self, method: str, params: list[Any]) -> Any:
        """Send a request and wait for the response synchronously.
        
        On Windows named pipes, concurrent read/write from different threads
        causes blocking issues. So we use a fully synchronous approach:
        1. Write the request
        2. Read messages until we get our response
        3. Handle any callback requests inline
        """
        with self._io_lock:
            request_id = self._next_id
            self._next_id += 1

            message = {
                "jsonrpc": "2.0",
                "id": request_id,
                "method": method,
                "params": params
            }
            self._write_message(message)

            # Read messages until we get our response
            while True:
                response = self._read_message()
                if response is None:
                    raise RuntimeError("Connection closed while waiting for response.")
                
                # Check if this is a callback request from the server
                if "method" in response:
                    self._handle_callback_request(response)
                    continue
                
                # This is a response - check if it's our response
                response_id = response.get("id")
                if response_id == request_id:
                    if "error" in response:
                        raise RuntimeError(response["error"].get("message", "RPC error"))
                    return response.get("result")
                # Response for a different request (shouldn't happen in sync mode)

    def _write_message(self, message: Dict[str, Any]) -> None:
        if not self._stream:
            raise RuntimeError("Not connected to AppHost.")
        body = json.dumps(message, separators=(",", ":")).encode("utf-8")
        header = f"Content-Length: {len(body)}\r\n\r\n".encode("utf-8")
        self._stream.write(header + body)
        self._stream.flush()

    def _handle_callback_request(self, message: Dict[str, Any]) -> None:
        """Handle a callback request from the server."""
        method = message.get("method")
        request_id = message.get("id")
        
        if method != "invokeCallback":
            if request_id is not None:
                self._write_message({
                    "jsonrpc": "2.0",
                    "id": request_id,
                    "error": {"code": -32601, "message": f"Unknown method: {method}"}
                })
            return

        params = message.get("params", [])
        callback_id = params[0] if len(params) > 0 else None
        args = params[1] if len(params) > 1 else None
        try:
            result = _invoke_callback(callback_id, args, self)
            self._write_message({"jsonrpc": "2.0", "id": request_id, "result": result})
        except Exception as exc:
            self._write_message({
                "jsonrpc": "2.0",
                "id": request_id,
                "error": {"code": -32000, "message": str(exc)}
            })

    def _read_message(self) -> Optional[Dict[str, Any]]:
        if not self._stream:
            return None
        headers: Dict[str, str] = {}
        while True:
            line = _read_line(self._stream)
            if not line:
                return None
            if line in (b"\r\n", b"\n"):
                break
            key, value = line.decode("utf-8").split(":", 1)
            headers[key.strip().lower()] = value.strip()
        length = int(headers.get("content-length", "0"))
        if length <= 0:
            return None
        body = _read_exact(self._stream, length)
        return json.loads(body.decode("utf-8"))


def _invoke_callback(callback_id: str, args: Any, client: AspireClient) -> Any:
    if callback_id is None:
        raise RuntimeError("Callback ID missing.")
    callback = _callback_registry.get(callback_id)
    if callback is None:
        raise RuntimeError(f"Callback not found: {callback_id}")

    positional_args: list[Any] = []
    if isinstance(args, dict):
        index = 0
        while True:
            key = f"p{index}"
            if key not in args:
                break
            positional_args.append(wrap_if_handle(args[key], client))
            index += 1
    elif args is not None:
        positional_args.append(wrap_if_handle(args, client))

    result = callback(*positional_args)
    if asyncio.iscoroutine(result):
        return asyncio.run(result)
    return result


def _read_exact(stream: Any, length: int) -> bytes:
    data = b""
    while len(data) < length:
        chunk = stream.read(length - len(data))
        if not chunk:
            raise EOFError("Unexpected end of stream.")
        data += chunk
    return data


def _read_line(stream: Any) -> bytes:
    """Read a line from the stream byte-by-byte.
    
    This is needed because readline() doesn't work reliably on Windows named pipes.
    We read byte-by-byte until we hit a newline.
    """
    line = b""
    while True:
        byte = stream.read(1)
        if not byte:
            return line if line else b""
        line += byte
        if byte == b"\n":
            return line


def _open_stream(socket_path: str) -> Any:
    """Open a stream to the AppHost server.
    
    On Windows, uses named pipes. On Unix, uses Unix domain sockets.
    """
    if os.name == "nt":
        pipe_path = f"\\\\.\\pipe\\{socket_path}"
        import io
        fd = os.open(pipe_path, os.O_RDWR | os.O_BINARY)
        return io.FileIO(fd, mode='r+b', closefd=True)
    sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    sock.connect(socket_path)
    return sock.makefile("rwb", buffering=0)
