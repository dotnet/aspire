#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/testproject/Directory.Packages.props", "tests/testproject/"]
COPY ["tests/testproject/Directory.Build.props", "tests/testproject/"]
COPY ["tests/Directory.Build.props", "tests/"]
COPY ["tests/testproject/Directory.Build.targets", "tests/testproject/"]
COPY ["tests/Directory.Build.targets", "tests/"]
COPY ["tests/testproject/TestProject.ServiceA/TestProject.ServiceA.csproj", "tests/testproject/TestProject.ServiceA/"]
RUN dotnet restore "./tests/testproject/TestProject.ServiceA/TestProject.ServiceA.csproj"
COPY . .
WORKDIR "/src/tests/testproject/TestProject.ServiceA"
RUN dotnet build "./TestProject.ServiceA.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TestProject.ServiceA.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestProject.ServiceA.dll"]