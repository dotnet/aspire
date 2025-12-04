from __future__ import annotations

from dataclasses import dataclass
import json
import os
import socket
import sys
import time
from typing import Any, Dict, Optional, Union, BinaryIO


def _default_unix_socket_path() -> str:
    runtime_dir = os.environ.get("XDG_RUNTIME_DIR")
    if runtime_dir and os.path.isdir(runtime_dir):
        return os.path.join(runtime_dir, "remote-app-host.sock")
    return "/tmp/remote-app-host.sock"


def make_instruction(name: str, /, **fields: Any) -> Dict[str, Any]:
    """Helper to build an instruction dict consistent with TS/C# clients.

    Example: make_instruction('CREATE_BUILDER', builderName='appBuilder1', args=[])
    """
    payload = {"name": name}
    payload.update(fields)
    return payload


@dataclass
class RemoteAppHostClient:
    pipe_name: str = "RemoteAppHost"
    unix_socket_path: Optional[str] = None
    _stream: Optional[Union[socket.socket, BinaryIO]] = None  # socket or file-like (Windows pipe)
    _connected: bool = False
    _is_socket: bool = False
    _next_id: int = 1
    debug: bool = False

    def _pipe_address(self) -> str:
        if os.name == "nt":
            # Windows named pipe path used by C# server
            return f"\\\\.\\pipe\\{self.pipe_name}"
        # Unix domain socket path
        return self.unix_socket_path or _default_unix_socket_path()

    # Low-level framing helpers -------------------------------------------------
    @staticmethod
    def _encode_message(obj: Dict[str, Any]) -> bytes:
        body = json.dumps(obj, separators=(",", ":")).encode("utf-8")
        header = f"Content-Length: {len(body)}\r\n\r\n".encode("ascii")
        return header + body

    def _read_exact(self, size: int) -> bytes:
        if not self._stream:
            raise ConnectionError("Not connected")
        chunks: list[bytes] = []
        remaining = size
        while remaining > 0:
            if self._is_socket:
                chunk = self._stream.recv(remaining)  # type: ignore[attr-defined]
            else:
                chunk = self._stream.read(remaining)  # type: ignore[attr-defined]
            if not chunk:
                raise ConnectionError("Connection closed while reading body")
            chunks.append(chunk)
            remaining -= len(chunk)
        return b"".join(chunks)

    def _read_message(self) -> Dict[str, Any]:
        # Read headers until blank line
        if not self._stream:
            raise ConnectionError("Not connected")
        header_bytes = b""
        recv_one = (self._stream.recv if self._is_socket else self._stream.read)  # type: ignore[attr-defined]
        while not header_bytes.endswith(b"\r\n\r\n"):
            b = recv_one(1)
            if not b:
                raise ConnectionError("Connection closed while reading header")
            header_bytes += b
        header_text = header_bytes.decode("ascii", errors="replace")
        content_length = 0
        for line in header_text.split("\r\n"):
            if line.lower().startswith("content-length:"):
                _, value = line.split(":", 1)
                content_length = int(value.strip())
                break
        if content_length <= 0:
            raise ValueError("Missing or invalid Content-Length header")
        body = self._read_exact(content_length)
        return json.loads(body.decode("utf-8"))

    # Connection management ------------------------------------------------------
    def connect(self, timeout: float = 5.0) -> None:
        if self._connected:
            return
        address = self._pipe_address()
        if os.name == "nt":
            # Use plain binary open on the named pipe path. Retry until timeout.
            start = time.time()
            last_err: Optional[Exception] = None
            while time.time() - start < timeout:
                try:
                    # 'r+b' for read/write, buffering=0 (unbuffered) to avoid partial flush issues
                    self._stream = open(address, 'r+b', buffering=0)
                    self._is_socket = False
                    self._connected = True
                    return
                except FileNotFoundError as e:  # server not ready yet
                    last_err = e
                    time.sleep(0.05)
                except PermissionError as e:  # race condition while creating
                    last_err = e
                    time.sleep(0.05)
            raise TimeoutError(f"Timed out connecting to named pipe '{address}': {last_err}")
        else:
            # Unix domain socket
            sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            sock.settimeout(timeout)
            sock.connect(address)
            sock.settimeout(None)
            self._stream = sock
            self._is_socket = True
            self._connected = True

    def disconnect(self) -> None:
        if self._stream:
            try:
                if self._is_socket:
                    self._stream.shutdown(socket.SHUT_RDWR)  # type: ignore[attr-defined]
            except Exception:  # noqa: BLE001
                pass
            try:
                self._stream.close()  # type: ignore[call-arg]
            finally:
                self._stream = None
        self._connected = False

    @property
    def connected(self) -> bool:  # Parity with TS client
        return self._connected

    # RPC methods ----------------------------------------------------------------
    def _send_request(self, method: str, *params: Any) -> Any:
        if not self._connected or not self._stream:
            raise ConnectionError("Not connected to RemoteAppHost")
        # Build JSON-RPC request manually to eliminate any ambiguity in param encoding.
        if params:
            # Positional params array; StreamJsonRpc will map by position.
            req_obj: Dict[str, Any] = {
                "jsonrpc": "2.0",
                "id": self._next_id,
                "method": method,
                "params": list(params),
            }
        else:
            req_obj = {"jsonrpc": "2.0", "id": self._next_id, "method": method}
        self._next_id += 1

        if self.debug:
            print("-->", json.dumps(req_obj, indent=2))

        data = self._encode_message(req_obj)
        if self._is_socket:
            self._stream.sendall(data)  # type: ignore[attr-defined]
        else:
            self._stream.write(data)  # type: ignore[attr-defined]
            self._stream.flush()  # type: ignore[attr-defined]
        resp = self._read_message()

        if self.debug:
            print("<--", json.dumps(resp, indent=2))

        if "error" in resp and resp["error"]:
            raise RuntimeError(f"RPC Error: {resp['error']}")
        return resp.get("result")

    def ping(self) -> str:
        return self._send_request("ping")

    def execute_instruction(self, instruction: Dict[str, Any]) -> Any:
        # Align with server signature: ExecuteInstructionAsync(string instructionJson)
        instruction_json = json.dumps(instruction)
        return self._send_request("executeInstruction", instruction_json)
