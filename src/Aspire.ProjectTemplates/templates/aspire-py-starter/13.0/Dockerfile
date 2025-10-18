# Stage 1: Build the Vite app
FROM node:22-slim AS frontend-stage

# Set the working directory inside the container
COPY frontend ./

WORKDIR /frontend
RUN npm install
RUN npm run build

# Stage 2: Build the Python application with UV
FROM ghcr.io/astral-sh/uv:python3.13-bookworm-slim AS builder

# Enable bytecode compilation and copy mode for the virtual environment
ENV UV_COMPILE_BYTECODE=1
ENV UV_LINK_MODE=copy

WORKDIR /app

# Install dependencies first for better layer caching
# Uses BuildKit cache mounts to speed up repeated builds
RUN --mount=type=cache,target=/root/.cache/uv --mount=type=bind,source=./api_service/uv.lock,target=uv.lock --mount=type=bind,source=./api_service/pyproject.toml,target=pyproject.toml \
    uv sync --locked --no-install-project --no-dev

# Copy the rest of the application source and install the project
COPY ./api_service /app
RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync --locked --no-dev

# Stage 3: Create the final runtime image
FROM python:3.13-slim-bookworm AS app

COPY --from=frontend-stage /dist /app/static

# ------------------------------
# 🚀 Runtime stage
# ------------------------------
# Create non-root user for security
RUN groupadd --system --gid 999 appuser && useradd --system --gid 999 --uid 999 --create-home appuser

# Copy the application and virtual environment from builder
COPY --from=builder --chown=appuser:appuser /app /app

# Add virtual environment to PATH and set VIRTUAL_ENV
ENV PATH=/app/.venv/bin:${PATH}
ENV VIRTUAL_ENV=/app/.venv
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

# Use the non-root user to run the application
USER appuser

# Set working directory
WORKDIR /app

# Run the application
ENTRYPOINT ["fastapi", "run", "app.py", "--host", "0.0.0.0", "--port", "8000"]
