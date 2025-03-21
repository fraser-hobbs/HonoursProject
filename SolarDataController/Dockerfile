# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy and restore dependencies
COPY SolarDataController.csproj ./
RUN dotnet restore --disable-parallel --verbosity normal

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
ENTRYPOINT ["./SolarDataController"]