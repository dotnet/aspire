# Integrating a Node.js app within a .NET Aspire application

This sample demonstrates an approach for integrating a Node.js app into a .NET Aspire application.

The app consists of two services:

- **NodeFrontend**: This is a simple Express-based Node.js app that renders a table of weather forecasts retrieved from a backend API and utilizes a Redis cache.
- **AspireWithNode.AspNetCoreApi**: This is an HTTP API that returns randomly generated weather forecast data.

## Pre-requisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js](https://nodejs.org) - at least version 20.9.0
- **Optional** [Visual Studio 2022](https://visualstudio.microsoft.com/vs/preview/)
