# syntax=docker/dockerfile:1
# -----------------------------------------------------------------------------
# Multi-stage Dockerfile for Python apps using uv
# Based on https://github.com/astral-sh/uv-docker-example
# - Stage 1 (builder): uses uv pre-installed image to install dependencies
# - Stage 2 (runtime): copies only the venv + app code for a slimmer image
# -----------------------------------------------------------------------------

# ------------------------------
# 🔧 Builder stage
# ------------------------------
ARG PYTHON_VERSION=3.12
ARG SCRIPT_NAME
FROM ghcr.io/astral-sh/uv:python${PYTHON_VERSION}-bookworm-slim AS builder

# Enable bytecode compilation and copy mode for the virtual environment
ENV UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy \
    UV_PYTHON_DOWNLOADS=0

WORKDIR /app

# Install dependencies first for better layer caching
# Uses BuildKit cache mounts to speed up repeated builds
RUN --mount=type=cache,target=/root/.cache/uv \
    --mount=type=bind,source=uv.lock,target=uv.lock \
    --mount=type=bind,source=pyproject.toml,target=pyproject.toml \
    uv sync --locked --no-install-project --no-dev

# Copy the rest of the application source and install the project
COPY . /app
RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync --locked --no-dev

# ------------------------------
# 🚀 Runtime stage
# ------------------------------
FROM python:${PYTHON_VERSION}-slim-bookworm AS app

# Create non-root user for security
RUN groupadd --system --gid 999 appuser \
    && useradd --system --gid 999 --uid 999 --create-home appuser

# Copy the application and virtual environment from builder
COPY --from=builder --chown=appuser:appuser /app /app

# Add virtual environment to PATH
ENV PATH="/app/.venv/bin:${PATH}" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

# Use the non-root user to run the application
USER appuser

# Set working directory
WORKDIR /app

# Run the application
ENTRYPOINT ["python"]
CMD ${SCRIPT_NAME}
