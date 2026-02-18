# EasyLog.Server - Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture](#architecture)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Usage](#usage)
6. [Communication Protocol](#communication-protocol)
7. [Deployment and Docker](#deployment-and-docker)
8. [Managing EasyLog Server on Docker](#managing-easylog-server-on-docker)

---

## Introduction

**EasyLog.Server** is a centralized log collection server that receives journalization events in real-time from remote clients via TCP and records them in local files.

### Key Features

- **TCP Server** : Listens for incoming connections on a configurable port
- **Asynchronous Reception** : Handles multiple clients in parallel without blocking
- **JSON/XML Format** : Receives and stores logs in JSON or XML format
- **Daily Rotation** : Automatically creates new log files each day
- **EasyLog.Lib Integration** : Uses the powerful EasyLog.Lib library for log management
- **Graceful Shutdown** : Supports graceful shutdown on `Ctrl+C`
- **Activity Logging** : Records server events (connections, disconnections, errors)

---

## Architecture

### Main Components

#### 1. **SocketServer**
Class responsible for the TCP server. Manages incoming connections, data reception, and storage.

**Responsibilities:**
- Listens for TCP connections
- Accepts remote clients
- Processes received JSON messages
- Writes logs via EasyLog.Lib
- Manages graceful shutdown

#### 2. **Program.cs**
Application entry point. Configures and starts the server.

**Responsibilities:**
- Reads environment variables (port, log directory)
- Creates and starts the server
- Handles `Ctrl+C` signal for clean shutdown

### Data Flow

```
Remote Client
     ↓
[Sends log JSON via TCP]
     ↓
SocketServer.HandleClientAsync()
     ↓
[Parses JSON]
     ↓
EasyLog.Write()
     ↓
Local log file (JSON/XML)
```

---

## Installation

### Requirements

- .NET SDK 10.0 or higher
- Available port (default: 5000)
- Write access to the log directory

### Compilation

```bash
# Restore dependencies and build
dotnet build EasyLog.Server.csproj

# Build in Release mode (recommended for production)
dotnet publish EasyLog.Server.csproj --configuration Release
```

---

## Configuration

### Environment Variables

The server is entirely configured via environment variables:

| Variable | Description | Default |
|----------|-------------|--------|
| `LOG_PORT` | TCP listening port | `5000` |
| `LOG_DIR` | Log storage directory | `./logs` |

### Configuration Examples

**Local Configuration (Development)**
```bash
set LOG_PORT=5000
set LOG_DIR=./logs
EasyLog.Server.exe
```

### Parameter Validation

- **Port** : Must be between 1 and 65535
- **Directory** : Will be created automatically if it doesn't exist

---

## Usage

### Starting the Server

**On Windows**
```bash
EasyLog.Server.exe
```

**On Linux/macOS**
```bash
./EasyLog.Server
```

### Console Output Example

```
Starting EasyLog Server...
Port: 5000
Log Directory: ./logs

[2025-02-18 14:30:45] EasyLog Server listening on port 5000
[2025-02-18 14:30:45] Log directory: ./logs
[2025-02-18 14:30:50] New client connected from 192.168.1.100:12345
[2025-02-18 14:30:52] Client disconnected (5 messages received)
[2025-02-18 14:35:10] New client connected from 192.168.1.101:54321
^C
[2025-02-18 14:35:15] Shutdown signal received
[2025-02-18 14:35:15] Server stopped
```

### Stopping the Server

Press `Ctrl+C` to gracefully shut down the server. The server will close log files and release resources before exiting.

---

## Communication Protocol

### Message Format

Clients send logs in JSON format, one line per message:

```json
{
  "timestamp": "2025-02-18T14:30:45",
  "name": "backup",
  "content": {
    "sourcePath": "C:\\Documents",
    "destinationPath": "D:\\Backups",
    "filesCount": 42,
    "status": "completed"
  }
}
```

### Expected Structure

**Required fields:**
- `timestamp` (DateTime) : Log timestamp in ISO 8601 format
- `name` (string) : Event category or name
- `content` (object) : Additional data

### Message Examples

**Authentication Message**
```json
{"timestamp":"2025-02-18T14:30:45","name":"auth","content":{"user":"admin","ip":"192.168.1.100","success":true}}
```

**Backup Message**
```json
{"timestamp":"2025-02-18T14:31:00","name":"backup","content":{"jobId":"job-1","status":"started","fileCount":150,"totalSize":"5.2 GB"}}
```

**Error Message**
```json
{"timestamp":"2025-02-18T14:31:15","name":"error","content":{"jobId":"job-1","errorCode":500,"message":"Destination directory not found"}}
```

### Error Handling

If a JSON message is invalid:
- The server logs the error to console
- Continues processing subsequent messages
- Does not close the client connection

Error example:
```
[2025-02-18 14:30:52] Invalid JSON from client: The JSON value could not be converted
```

---

## Deployment and Docker

### STEP 1: Publish Linux Executable

This is the most important step: compile the server for Linux.

```bash
cd "C:/Users/Noiret/Documents/2 - Ressources/3.2 - Etudes/B2-genie_logiciel/0-PROJET/EasySave"

dotnet publish EasyLog.Server/EasyLog.Server.csproj -c Release -o ./EasyLog.Server/bin/Release/net10.0/publish -r linux-x64 --self-contained
```

What it does:
- `-c Release` : Compiles in production mode (optimized)
- `-o ./EasyLog.Server/bin/Release/net10.0/publish` : Output folder
- `-r linux-x64` : Creates a Linux executable (important!)
- `--self-contained` : Includes .NET 10 runtime in the executable

Expected result:
```
Path: C:\Users...\EasyLog.Server\bin\Release\net10.0\publish\
```

---

### STEP 2: Verify Linux Executable Exists

```bash
ls "EasyLog.Server/bin/Release/net10.0/publish/EasyLog.Server"
```

You should see a file named EasyLog.Server (without .exe) approximately 77-80 KB.

Verify it's a real Linux binary:
```bash
file "EasyLog.Server/bin/Release/net10.0/publish/EasyLog.Server"
```

Expected result:
```
ELF 64-bit LSB pie executable, x86-64, version 1 (SYSV), dynamically linked
```

---

### STEP 3: Build Docker Image

```bash
docker build -t easysave/easylog-server:latest -f EasyLog.Server/Dockerfile .
```

What it does:
- `-t easysave/easylog-server:latest` : Image name + tag
- `-f EasyLog.Server/Dockerfile` : Use this Dockerfile
- `.` : Context = current folder

Expected result:
```
[10] exporting to image
[10] naming to docker.io/easysave/easylog-server:latest done
[10] unpacking to docker.io/easysave/easylog-server:latest done
DONE 7.1s
```

---

## Managing EasyLog Server on Docker

### 1. START the server

First time (create + launch container):
```bash
docker run -d -p 5000:5000 -v easylog-volume:/logs --name easylog-server easysave/easylog-server:latest
```

Verify it's running:
```bash
docker ps
```

See startup logs:
```bash
docker logs easylog-server
```

---

### 2. STOP the server

```bash
docker stop easylog-server
```

Verify it's stopped:
```bash
docker ps -a
```

---

### 3. RESTART the server

If already created (stopped):
```bash
docker start easylog-server
```

Verify:
```bash
docker logs easylog-server
```

---

### 4. VIEW LOGS

Full logs:
```bash
docker logs easylog-server
```

Real-time logs (continuous follow):
```bash
docker logs -f easylog-server
```

Last 50 lines:
```bash
docker logs --tail 50 easylog-server
```

Logs with timestamps:
```bash
docker logs -t easylog-server
```

---

### 5. ACCESS LOG FILES

View files in the volume:
```bash
docker run --rm -v easylog-volume:/logs alpine:latest ls -la /logs
```

View log file content:
```bash
docker run --rm -v easylog-volume:/logs alpine:latest cat /logs/fileName.json
```

Copy logs to your machine:
```bash
docker run --rm -v easylog-volume:/logs -v C:\logs-export:/export alpine:latest cp -r /logs/* /export/
```

---

### 6. CLEANUP / DELETE

Stop + remove container (keeps logs):
```bash
docker rm -f easylog-server
```

Also delete logs (volume):
```bash
docker volume rm easylog-volume
```

Delete everything (container + volume + image):
```bash
docker rm -f easylog-server
docker volume rm easylog-volume
docker rmi easysave/easylog-server:latest
```

---

### 7. TEST CONNECTION

Edit config.json:
```json
"easyLogServer": {
  "enabled": true,
  "mode": "server_only",
  "host": "localhost",
  "port": 5000
}
```

Launch a test job:
```bash
dotnet run --project EasySave.Cli/EasySave.Cli.csproj -- --config-path config.json --job test
```

Verify the server received the logs:
```bash
docker logs easylog-server
```

---

### 8. CHECK STATUS

Container status:
```bash
docker ps -a | grep easylog-server
```

Detailed info:
```bash
docker inspect easylog-server
```

Resource usage:
```bash
docker stats easylog-server
```

---
