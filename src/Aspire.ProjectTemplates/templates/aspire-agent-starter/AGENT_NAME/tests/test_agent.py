import os

import pytest

from {{cookiecutter.project_slug}} import run_agent


def test_run_agent_requires_endpoint(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("FOUNDRY_PROJECT_ENDPOINT", raising=False)

    with pytest.raises(SystemExit, match="FOUNDRY_PROJECT_ENDPOINT is required."):
        run_agent()
