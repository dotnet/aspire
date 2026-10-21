"""Branch-only premerge recovery driver; no model waits for CI."""

import base64
import json
import os
import re
import subprocess
import sys
import time


REPOSITORY = "microsoft/aspire"
ISSUE_URL = os.environ["ISSUE_URL"]
MATCH = re.fullmatch(r"https://github\.com/microsoft/aspire/issues/([1-9][0-9]*)", ISSUE_URL)
if not MATCH or os.environ["GITHUB_REPOSITORY"] != REPOSITORY:
    raise ValueError("The hosted validation accepts only an exact upstream issue URL")
ISSUE = MATCH[1]


def api(endpoint, payload=None):
    command = ["gh", "api", f"repos/{REPOSITORY}/{endpoint}"]
    if payload is not None:
        command += ["--method", "POST", "--input", "-"]
    result = subprocess.run(command, input=json.dumps(payload) if payload else None,
                            text=True, capture_output=True, check=True)
    return json.loads(result.stdout) if result.stdout else None


def checkpoint():
    reference = api(f"git/ref/heads/ankj/flaky-test-{ISSUE}-state")
    revision = reference["object"]["sha"]
    document = api(f"contents/state.json?ref={revision}")
    state = json.loads(base64.b64decode(document["content"]))
    if (state["issue_url"] != ISSUE_URL
            or state["control_sha"] != os.environ["GITHUB_SHA"]
            or state["agent_sha"] != os.environ["FLAKY_AGENT_SHA"]
            or state["worker_sha"] != os.environ["FLAKY_WORKER_SHA"]
            or state["agent_workflow"] != "reproduce-flaky-tests.yml"):
        raise ValueError("Checkpoint changed its enrolled investigation identity")
    return state, revision


subprocess.run([sys.executable, ".github/scripts/flaky_investigation/controller.py", "advance"],
               check=True)
state, revision = checkpoint()
print(json.dumps({"checkpoint": revision, "phase": state["phase"],
                  "outcome": state.get("outcome")}), flush=True)
if state["phase"] == "terminal":
    sys.exit(0)
if state["phase"] not in {"agent", "worker", "ready", "candidate-ready"}:
    raise ValueError("Uncertain checkpoint; refusing automatic redispatch")
owned = state.get(state["phase"]) if state["phase"] in {"agent", "worker"} else None
if owned is not None:
    # Only a deterministic Actions step waits. It reads the exact run recorded
    # by the controller, never searches for the newest run or cancels other CI.
    deadline = time.monotonic() + 45 * 60
    while True:
        run = api(f"actions/runs/{owned['id']}")
        if (run["head_sha"] != owned["sha"]
                or run["path"].split("@", 1)[0].rsplit("/", 1)[-1]
                != owned["workflow"].rsplit("/", 1)[-1]):
            raise ValueError("Owned run identity changed while awaiting completion")
        if run["status"] == "completed":
            print(json.dumps({"owned_run": run["id"], "conclusion": run["conclusion"]}),
                  flush=True)
            break
        if time.monotonic() >= deadline:
            raise TimeoutError("Owned run still active; state retained for explicit recovery")
        time.sleep(30)
current, current_revision = checkpoint()
if current["phase"] == "terminal":
    sys.exit(0)
if current_revision != revision:
    raise ValueError("Checkpoint advanced elsewhere; refusing a duplicate handoff")
# A fresh hosted controller run owns the next phase. Persist the dispatch
# response in job output; a missing receipt is an error, never a retry.
receipt = api("actions/workflows/reproduce-flaky-tests.yml/dispatches", {
    "ref": os.environ["GITHUB_REF_NAME"],
    "inputs": {"issue_url": ISSUE_URL, "operation": "resume"},
    "return_run_details": True,
})
if not isinstance(receipt, dict) or not isinstance(receipt.get("workflow_run_id"), int):
    raise RuntimeError("Controller handoff has no run-ID receipt; reconcile instead of retrying")
print(json.dumps({"next_controller": receipt}), flush=True)
