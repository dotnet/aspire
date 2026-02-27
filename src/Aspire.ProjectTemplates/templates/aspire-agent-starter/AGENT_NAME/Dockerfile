FROM --platform=linux/amd64 python:3.12-slim

WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

COPY pyproject.toml uv.lock ./
RUN uv sync --no-dev --locked --no-install-project

COPY . .
RUN uv sync --no-dev --locked

ENTRYPOINT ["uv", "run"]
CMD ["python", "./main.py"]
