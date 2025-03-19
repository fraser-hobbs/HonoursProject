FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production 

# Set user only if APP_UID is defined
ARG APP_UID
RUN if [ -n "$APP_UID" ]; then useradd -m -u $APP_UID appuser && chown -R appuser /app; fi
USER ${APP_UID:-root}

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ✅ Fix COPY path to match GitHub workflow context
COPY SolarDataController/SolarDataController.csproj SolarDataController/
RUN dotnet restore SolarDataController/SolarDataController.csproj

# ✅ Ensure everything is copied correctly
COPY SolarDataController/ SolarDataController/
WORKDIR /src/SolarDataController
RUN dotnet build SolarDataController.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish SolarDataController.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SolarDataController.dll"]
