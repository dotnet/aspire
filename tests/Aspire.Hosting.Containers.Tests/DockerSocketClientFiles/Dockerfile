FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Program.cs .
COPY docker-client.csproj .
RUN dotnet restore "docker-client.csproj"
RUN dotnet publish "docker-client.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "docker-client.dll"]