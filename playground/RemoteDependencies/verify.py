#!/usr/bin/env python3
"""Manual, destructive-to-owned-fixture-only integration checks. Run fixture.py up first."""
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import threading
import time
import urllib.error
import urllib.request
import uuid

import fixture

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[1]
DOTNET = REPO / ".dotnet/dotnet"
APPHOST = REPO / "artifacts/bin/RemoteDependencies.AppHost/Debug/net10.0/RemoteDependencies.AppHost.dll"
ENV = json.loads((fixture.STATE / "env.json").read_text())
# The nested bridge is unavailable in some containerized developer environments.
ENV["REMOTE_HOST_NETWORK_PORT"] = "26379"
STATE = fixture.load()
fixture.verify_owner(STATE)
CONTAINER = STATE["container"]


def remote(*args):
    return fixture.run("docker", "exec", CONTAINER, "docker", *args)


def wait_for(predicate, description, timeout=45):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        value = predicate()
        if value:
            return value
        time.sleep(0.1)
    raise AssertionError("Timed out: " + description)


class Session:
    def __init__(self, name, **environment):
        self.events = []
        self.path = fixture.STATE / f"verify-{name}.log"
        self.log = self.path.open("w")
        self.process = subprocess.Popen(
            [str(DOTNET), str(APPHOST)], cwd=REPO,
            env={**os.environ, **ENV, "REMOTE_HEADLESS": "1", **environment},
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        self.reader = threading.Thread(target=self.read, daemon=True)
        self.reader.start()

    def read(self):
        for line in self.process.stdout:
            self.log.write(line)
            self.log.flush()
            if line.startswith("{"):
                self.events.append(json.loads(line))

    def state(self, state):
        return any(e.get("kind") == "state" and e.get("resource") == "cache" and
                   e.get("state") == state for e in self.events)

    def content(self, text):
        return any(text in e.get("content", "") for e in self.events)

    def stop(self):
        if self.process.poll() is None:
            self.process.send_signal(signal.SIGINT)
        try:
            code = self.process.wait(timeout=35)
        except subprocess.TimeoutExpired:
            self.process.kill()
            self.process.wait()
            raise AssertionError(f"AppHost failed to stop gracefully; see {self.path}")
        finally:
            self.reader.join(timeout=3)
            self.log.close()
        assert code == 0, f"AppHost exit {code}; see {self.path}"


def request():
    try:
        with urllib.request.urlopen("http://127.0.0.1:18080/", timeout=5) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, json.load(error)
    except (urllib.error.URLError, TimeoutError):
        return None


def owned():
    return remote("ps", "-aq", "--filter", "label=aspire.remote.owner")


def clean():
    assert owned() == "", "Owned remote container leaked"


def healthy(session):
    wait_for(lambda: session.state("Running"), f"Redis Running; see {session.path}")
    response = wait_for(request, "local HTTP API listening")
    assert response[0] == 200, response
    assert response[1]["application"] == "local"
    assert response[1]["cache"] == "127.0.0.1:16379"
    assert session.content("Redis PONG verified")
    return response[1]


def verify():
    clean()
    initial_volumes = set(remote("volume", "ls", "-q").splitlines())
    sentinel = remote("run", "-d", "--name", "aspire-remote-unrelated-" + uuid.uuid4().hex,
                      "redis:7-alpine")
    results = []
    session = None
    try:
        session = Session("happy")
        first = healthy(session)
        with socket.socket() as not_remote_loopback:
            not_remote_loopback.settimeout(1)
            assert not_remote_loopback.connect_ex(("127.0.0.1", 26379)) != 0, "Remote loopback leaked into local namespace"
        second = request()[1]
        assert second["requests"] == first["requests"] + 1
        cache = owned()
        assert remote("exec", cache, "redis-cli", "-p", "26379", "get",
                      "remote-development:requests") == str(second["requests"])
        assert session.content("Ready to accept connections")
        assert remote("inspect", "--format", "{{.HostConfig.NetworkMode}}", cache) == "host"
        session.stop()
        assert session.state("Finished")
        session = None
        clean()
        assert remote("inspect", "--format", "{{.State.Running}}", sentinel) == "true"
        results.append({"scenario": "local HTTP -> SSH -> real Redis; logs/status/cleanup/ownership", "passed": True,
                        "responses": [first, second]})

        session = Session("disconnect")
        healthy(session)
        # Kill exactly the tunnel client owned by this AppHost, not any unrelated SSH process.
        rows = fixture.run("ps", "-eo", "pid=,ppid=,args=").splitlines()
        pids = [int(parts[0]) for row in rows if len(parts := row.split(maxsplit=2)) == 3
                and int(parts[1]) == session.process.pid
                and "ASPIRE_SSH_FORWARD_READY" in parts[2] and parts[2].startswith("ssh ")]
        assert len(pids) == 1, pids
        os.kill(pids[0], signal.SIGTERM)
        wait_for(lambda: session.state("Unavailable"), "disconnect diagnosed")
        assert wait_for(request, "local API failure")[0] == 503
        wait_for(lambda: session.content("Remote cleanup verified"), "cleanup after SSH disconnect")
        session.stop()
        session = None
        clean()
        results.append({"scenario": "SSH tunnel disconnect -> Unavailable + HTTP 503 + scoped cleanup", "passed": True})

        session = Session("restart")
        healthy(session)
        session.stop()
        session = None
        clean()
        results.append({"scenario": "explicit AppHost restart restores remote development", "passed": True})

        with socket.socket() as collision:
            collision.bind(("127.0.0.1", 0))
            collision.listen()
            session = Session("local-port-collision", REMOTE_LOCAL_PORT=str(collision.getsockname()[1]))
            wait_for(lambda: session.state("FailedToStart"), "local forward bind failure")
            assert session.content("cannot listen to port") or session.content("Address already in use")
            session.stop()
            session = None
        clean()
        results.append({"scenario": "occupied local tunnel port -> FailedToStart, no false readiness", "passed": True})

        session = Session("bad-image", REMOTE_IMAGE="redis:aspire-poc-nonexistent-tag")
        wait_for(lambda: session.state("FailedToStart"), "remote image startup failure")
        session.stop()
        session = None
        clean()
        results.append({"scenario": "remote image startup failure -> FailedToStart + cleanup", "passed": True})

        session = Session("remote-outage")
        healthy(session)
        leaked = owned()
        subprocess.run(["docker", "stop", "-t", "2", CONTAINER], check=True, stdout=subprocess.DEVNULL)
        wait_for(lambda: session.state("Unavailable"), "remote machine outage")
        assert wait_for(request, "local API outage")[0] == 503
        wait_for(lambda: session.content("Remote cleanup unconfirmed"), "explicit cleanup uncertainty", timeout=25)
        session.stop()
        session = None
        results.append({"scenario": "remote machine outage -> Unavailable + 503 + explicit cleanup command", "passed": True})
    finally:
        try:
            if session is not None:
                session.stop()
        finally:
            subprocess.run(["docker", "start", CONTAINER], check=True, stdout=subprocess.DEVNULL)
            wait_for(lambda: subprocess.run(["docker", "exec", CONTAINER, "docker", "info"],
                                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0,
                     "fixture engine restored")
            # The outage deliberately prevents AppHost cleanup; reconcile only that exact owned ID.
            if "leaked" in locals() and leaked:
                owner = remote("inspect", "--format", '{{index .Config.Labels "aspire.remote.owner"}}', leaked)
                assert len(owner) == 32 and remote("inspect", "--format", "{{.Name}}", leaked) == "/aspire-remote-" + owner
                remote("rm", "-fv", leaked)
            remote("rm", "-fv", sentinel)
    clean()
    assert set(remote("volume", "ls", "-q").splitlines()) == initial_volumes, "Anonymous Redis volume leaked"
    evidence = {"topology": "local AppHost/API -> loopback SSH -> isolated SSH fixture -> host-network nested Redis",
                "wan_tested": False, "anonymous_volume_leaks": 0, "scenarios": results}
    (fixture.STATE / "evidence.json").write_text(json.dumps(evidence, indent=2) + "\n")
    print(json.dumps(evidence, indent=2))


if __name__ == "__main__":
    verify()
