# Self-contained .NET 10 application (includes runtime)
FROM alpine:latest

WORKDIR /app

# Install required dependencies for .NET
RUN apk add --no-cache libstdc++ libintl libc6-compat icu-libs

# Copy self-contained .NET 10 application
COPY ./EasyLog.Server/bin/Release/publish .

# Create logs directory
RUN mkdir -p /logs

# Expose port
EXPOSE 5000

# Environment variables
ENV LOG_PORT=5000
ENV LOG_DIR=/logs

# Volume for logs
VOLUME ["/logs"]

# Run the application directly (self-contained)
ENTRYPOINT ["./EasyLog.Server"]
