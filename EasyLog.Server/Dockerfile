# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:10 AS build

WORKDIR /src

# Copy project files
COPY ["EasyLog.Lib/EasyLog.Lib.csproj", "EasyLog.Lib/"]
COPY ["EasyLog.Server/EasyLog.Server.csproj", "EasyLog.Server/"]

# Restore dependencies
RUN dotnet restore "EasyLog.Server/EasyLog.Server.csproj"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish "EasyLog.Server/EasyLog.Server.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10

WORKDIR /app

# Copy published binaries
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /logs

# Expose port
EXPOSE 5000

# Environment variables
ENV LOG_PORT=5000
ENV LOG_DIR=/logs

# Volume for logs
VOLUME ["/logs"]

# Run the server
ENTRYPOINT ["./EasyLog.Server"]
