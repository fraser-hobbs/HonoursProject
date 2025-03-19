FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
ENV DOTNET_ENVIRONMENT=Production 
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SolarDataController/SolarDataController.csproj", "SolarDataController/"]
RUN dotnet restore "SolarDataController/SolarDataController.csproj"
COPY . .
WORKDIR "/src/SolarDataController"
RUN dotnet build "SolarDataController.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SolarDataController.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SolarDataController.dll"]
