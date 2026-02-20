.PHONY: dev setup test lint typecheck check clean

setup:
	uv sync --locked

.setup-check:
	uv sync --check

dev: .setup-check
	uv run python ./main.py

test: .setup-check
	uv run pytest tests/

lint: .setup-check
	uv run ruff src/ tests/

clean:
	rm -rf __pycache__ .pytest_cache .ruff_cache *.egg-info

check: .setup-check lint typecheck test
