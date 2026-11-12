# syntax=docker/dockerfile:1

# ---- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, copying only the files that affect it, so the layer is cached
# until a project file or package version changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/RestaurantCopilot.Domain/RestaurantCopilot.Domain.csproj src/RestaurantCopilot.Domain/
COPY src/RestaurantCopilot.Application/RestaurantCopilot.Application.csproj src/RestaurantCopilot.Application/
COPY src/RestaurantCopilot.Infrastructure/RestaurantCopilot.Infrastructure.csproj src/RestaurantCopilot.Infrastructure/
COPY src/RestaurantCopilot.Api/RestaurantCopilot.Api.csproj src/RestaurantCopilot.Api/
RUN dotnet restore src/RestaurantCopilot.Api/RestaurantCopilot.Api.csproj

COPY src/ src/
RUN dotnet publish src/RestaurantCopilot.Api/RestaurantCopilot.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The official image ships a non-root "app" user; never run the API as root.
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestaurantCopilot.Api.dll"]
