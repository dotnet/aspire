#!/usr/bin/env python3
"""Own a disposable, loopback-only SSH + nested Docker fixture. Requires Linux Docker."""
import json
import os
from pathlib import Path
import shlex
import socket
import subprocess
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parent
STATE = ROOT / ".fixture"


def run(*args):
    return subprocess.check_output(args, text=True).strip()


def load():
    return json.loads((STATE / "state.json").read_text())


def verify_owner(state):
    owner = run("docker", "inspect", "--format",
                '{{index .Config.Labels "aspire.remote.fixture"}}', state["container"])
    if owner != state["owner"]:
        raise RuntimeError("Refusing to operate on a fixture not owned by this state file.")


def up():
    run("docker", "info")
    if (STATE / "state.json").exists():
        verify_owner(load())
        print("Existing fixture retained; use .fixture/env.sh or run fixture.py down first.")
        return
    STATE.mkdir(mode=0o700, exist_ok=True)
    os.chmod(STATE, 0o700)
    key = STATE / "id_ed25519"
    if not key.exists():
        run("ssh-keygen", "-q", "-t", "ed25519", "-N", "", "-f", str(key))
    owner = uuid.uuid4().hex
    image = f"aspire-remote-fixture:{owner}"
    subprocess.run(["docker", "build", "-q", "-t", image, str(ROOT / "fixture")], check=True)
    # Docker can reassign an automatically published port on container restart.
    # Request a concrete free port so the outage scenario retains its pinned SSH endpoint.
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 0))
        ssh_port = reservation.getsockname()[1]
    container = run("docker", "run", "-d", "--privileged",
                    "--label", f"aspire.remote.fixture={owner}",
                    "--name", f"aspire-ssh-fixture-{owner}",
                    "-p", f"127.0.0.1:{ssh_port}:22",
                    "--mount", f"type=bind,src={key}.pub,dst=/root/.ssh/authorized_keys,readonly",
                    image)
    (STATE / "state.json").write_text(json.dumps(
        {"container": container, "owner": owner, "image": image}, indent=2))
    deadline = time.monotonic() + 90
    while subprocess.run(["docker", "exec", container, "docker", "info"],
                         stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode:
        if time.monotonic() > deadline:
            raise RuntimeError("Nested Docker did not start; inspect docker logs, then fixture.py down.")
        time.sleep(1)
    mapping = run("docker", "port", container, "22/tcp")
    if not mapping.startswith("127.0.0.1:"):
        raise RuntimeError(f"Unexpected SSH binding: {mapping}")
    port = mapping.split(":")[1]
    # Pin via Docker's authenticated local control channel, never trust ssh-keyscan.
    public_key = run("docker", "exec", container, "cat", "/etc/ssh/ssh_host_ed25519_key.pub")
    known_hosts = STATE / "known_hosts"
    known_hosts.write_text(f"[127.0.0.1]:{port} {public_key}\n")
    env = {"REMOTE_HOST": "127.0.0.1", "REMOTE_SSH_PORT": port, "REMOTE_USER": "root",
           "REMOTE_IDENTITY": str(key), "REMOTE_KNOWN_HOSTS": str(known_hosts),
           "REMOTE_HOST_NETWORK_PORT": "26379"}
    (STATE / "env.json").write_text(json.dumps(env, indent=2))
    (STATE / "env.sh").write_text("".join(f"export {k}={shlex.quote(v)}\n" for k, v in env.items()))
    # Prepull a real dependency in the separate engine; no public engine TCP listener exists.
    subprocess.run(["docker", "exec", container, "docker", "pull", "redis:7-alpine"], check=True)
    print(f"Fixture ready: SSH {mapping}; source {STATE / 'env.sh'}")


def down():
    state = load()
    verify_owner(state)
    subprocess.run(["docker", "rm", "-fv", state["container"]], check=True)
    subprocess.run(["docker", "image", "rm", state["image"]], check=True)
    for name in ["state.json", "env.json", "env.sh", "id_ed25519", "id_ed25519.pub", "known_hosts"]:
        (STATE / name).unlink(missing_ok=True)
    print("Owned fixture, nested containers and ephemeral SSH keys removed.")


if __name__ == "__main__":
    if sys.argv[1:] == ["up"]:
        up()
    elif sys.argv[1:] == ["down"]:
        down()
    else:
        raise SystemExit("Usage: python3 fixture.py up|down")
