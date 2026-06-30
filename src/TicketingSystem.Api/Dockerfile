# syntax=docker/dockerfile:1
#
# Multi-stage build for the ASP.NET Core 10 API.
# IMPORTANT: build context must be the REPO ROOT — the build needs the shared
# Directory.Build.props / Directory.Packages.props (CPM) and all four src/*
# projects. Compose sets `context: .` with `dockerfile: src/TicketingSystem.Api/Dockerfile`.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Centralised build configuration first so restore can resolve package versions.
COPY Directory.Build.props Directory.Packages.props ./

# Project files only — enables a layer-cached restore that survives source edits.
COPY src/TicketingSystem.Domain/TicketingSystem.Domain.csproj                 src/TicketingSystem.Domain/
COPY src/TicketingSystem.Application/TicketingSystem.Application.csproj        src/TicketingSystem.Application/
COPY src/TicketingSystem.Infrastructure/TicketingSystem.Infrastructure.csproj src/TicketingSystem.Infrastructure/
COPY src/TicketingSystem.Api/TicketingSystem.Api.csproj                       src/TicketingSystem.Api/
RUN dotnet restore src/TicketingSystem.Api/TicketingSystem.Api.csproj

# Now the rest of the source, then publish.
COPY src/ src/
RUN dotnet publish src/TicketingSystem.Api/TicketingSystem.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is needed by the HEALTHCHECK below (not present in the runtime image).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=40s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TicketingSystem.Api.dll"]
