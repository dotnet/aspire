# syntax=docker/dockerfile:1.7
ARG PYTHON_VERSION=3.12
ARG SCRIPT_NAME
# --- Stage 1: build dependencies & wheels
FROM python:${PYTHON_VERSION}-slim AS build

RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential gcc && rm -rf /var/lib/apt/lists/*

WORKDIR /build
COPY requirements.txt ./

# Build wheels for caching & reproducibility
RUN pip install --upgrade pip wheel setuptools \
    && pip wheel --wheel-dir /wheels -r requirements.txt


# --- Stage 2: runtime image
FROM python:${PYTHON_VERSION}-slim AS app

RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/* && \
    useradd -m -u 1000 appuser && \
    mkdir -p /app && \
    chown -R appuser:appuser /app

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

WORKDIR /app

# Copy pre-built wheels from builder
COPY --from=build /wheels /wheels
COPY requirements.txt ./

# Install from local wheel cache (fast, no network)
RUN pip install --no-index --find-links=/wheels -r requirements.txt && \
    rm -rf /wheels

COPY . .

USER appuser

ENTRYPOINT ["python"]
CMD ${SCRIPT_NAME}
