# Aspire Python Starter Template

This template creates an Aspire application with a Python backend API service and a JavaScript/React frontend using Vite.

## What's Included

This template includes:

- **Python API Service** (`api_service/`): A FastAPI-based backend service with Redis caching support and OpenTelemetry instrumentation
- **React Frontend** (`frontend/`): A Vite + React + TypeScript frontend application
- **AppHost** (`apphost.cs`): Single-file Aspire app host that orchestrates both resources

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Python 3.13](https://www.python.org/downloads/) or later
- [Node.js 18](https://nodejs.org/) or later
- [uv](https://docs.astral.sh/uv/) - Python package installer (recommended)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Redis container)

## Getting Started

1. Create a new project using this template:

   ```bash
   dotnet new aspire-py-starter -n MyApp
   cd MyApp
   ```

2. Run the application:

   ```bash
   dotnet run --project apphost.cs
   ```

   Or use the Aspire Dashboard:

   ```bash
   dotnet run apphost.cs
   ```

3. The Aspire Dashboard will open in your browser. From there you can:
   - View the running services
   - Access logs and traces
   - Monitor resource health

## Project Structure

```
.
├── api_service/          # Python FastAPI backend
│   ├── .python-version   # Python version specification
│   ├── app.py           # Main FastAPI application
│   ├── pyproject.toml   # Python project configuration
│   ├── telemetry.py     # OpenTelemetry configuration
│   └── uv.lock          # Python dependency lock file
├── frontend/            # React + Vite frontend
│   ├── public/          # Static assets
│   ├── src/            # React source code
│   ├── index.html      # HTML entry point
│   ├── package.json    # Node.js dependencies
│   └── vite.config.ts  # Vite configuration
└── apphost.cs          # Aspire app host
```

## Python API Service

The Python service is a FastAPI application that:

- Exposes a `/api/weatherforecast` endpoint that returns weather data
- Uses Redis for caching responses
- Instruments telemetry using OpenTelemetry
- Serves static files from the frontend build output

### Running Locally

```bash
cd api_service
uv sync
uv run python app.py
```

The API will be available at `http://localhost:8111` by default.

## Frontend

The frontend is a Vite-powered React application with TypeScript that:

- Displays weather forecast data from the API
- Uses modern React patterns and hooks
- Includes hot module replacement for fast development

### Running Locally

```bash
cd frontend
npm install
npm run dev
```

The frontend development server will be available at `http://localhost:5173` by default.

## Configuration

### Environment Variables

The API service supports the following environment variables:

- `PORT`: HTTP port for the API service (default: 8111)
- `HOST`: Host address to bind to (default: 127.0.0.1)
- `DEBUG`: Enable debug mode and hot reload (default: False)
- `CACHE_URI`: Redis connection string (automatically configured by Aspire)

### Aspire Configuration

The `apphost.cs` file configures:

- Redis cache resource
- Python API service with uv environment
- Vite frontend application
- Service references and dependencies

## Customization

### Adding Python Dependencies

1. Edit `api_service/pyproject.toml`
2. Run `uv sync` to update the lock file

### Adding JavaScript Dependencies

1. Edit `frontend/package.json`
2. Run `npm install` to update the lock file

### Adding New Services

Edit `apphost.cs` and use the Aspire hosting APIs to add new resources:

```csharp
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("mydb");

var newService = builder.AddPythonScript("newservice", "./new_service", "main.py")
    .WithReference(db);
```

## Deployment

This template includes a Dockerfile configuration for the Python service. You can publish the application using:

```bash
dotnet publish
```

The Aspire tooling will generate deployment manifests for your target environment.

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Aspire Python Hosting](https://learn.microsoft.com/dotnet/aspire/get-started/build-aspire-apps-with-python)
- [Aspire Node.js Hosting](https://learn.microsoft.com/dotnet/aspire/get-started/build-aspire-apps-with-nodejs)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Vite Documentation](https://vite.dev/)
- [React Documentation](https://react.dev/)
