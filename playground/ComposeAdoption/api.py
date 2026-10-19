# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

import json
import os
import socket
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse


class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        redis = urlparse(os.environ["REDIS_URL"])
        with socket.create_connection((redis.hostname, redis.port), timeout=5) as connection:
            connection.sendall(b"*2\r\n$4\r\nINCR\r\n$6\r\nvisits\r\n")
            with connection.makefile("rb") as response:
                count = response.readline()
        if not count.startswith(b":"):
            raise RuntimeError(f"Redis INCR failed: {count!r}")
        body = json.dumps({"implementation": "compose-python", "visits": int(count[1:])}).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


ThreadingHTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
