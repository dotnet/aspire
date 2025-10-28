# Flask App Example for Aspire

This is a simple Flask application that demonstrates integration with .NET Aspire using Gunicorn in production and Flask's dev server in development.

## Features

- Application factory pattern (`create_app()`)
- Multiple endpoints (/, /health, /api/data)
- OpenTelemetry instrumentation
- JSON responses
- Gunicorn for production, Flask dev server for development

## Endpoints

- `GET /` - Hello world endpoint
- `GET /health` - Health check endpoint
- `GET /api/data` - Returns sample data

## Running with Aspire

This app is configured to run via the Python.AppHost project using `AddGunicornApp`:

```csharp
var flaskApp = builder.AddGunicornApp("flask-app", "../flask_app", "app:create_app")
    .WithUvEnvironment();
```

In development mode (non-publish), this uses Flask's development server with auto-reload.
In production mode (publish), this uses Gunicorn for better performance.

## Local Development

1. Create virtual environment:
   ```bash
   uv venv
   ```

2. Install dependencies:
   ```bash
   uv sync
   ```

3. Run the app:
   ```bash
   flask --app app:create_app run --debug
   ```

Or run directly:
```bash
python app.py
```
