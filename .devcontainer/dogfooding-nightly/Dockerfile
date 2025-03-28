# Base image
FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Pre-create the workspace folder and the NuGet.config file
RUN mkdir -p /workspaces/dogfood &&\
    cd /workspaces/dogfood &&\
    dotnet new nugetconfig &&\
    dotnet nuget add source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json -n nightly-feed
