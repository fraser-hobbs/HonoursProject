# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project file
COPY BuildingDataController.csproj ./

# Set up authentication for GitHub Packages
ARG GITHUB_PAT
ARG GITHUB_USR
RUN dotnet nuget add source "https://nuget.pkg.github.com/fraser-hobbs/index.json" \
    --username "$GITHUB_USR" \
    --password "$GITHUB_PAT" \
    --store-password-in-clear-text

# Copy the remaining source code and build the application
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim
WORKDIR /app

# Copy the application from the build stage
COPY --from=build /app/out .

# Set up a non-root user
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser

# Run the application
ENTRYPOINT ["./BuildingDataController"]