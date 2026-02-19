# EasyLog.Lib - Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Architecture](#architecture)
4. [The 3 Logging Modes](#the-3-logging-modes)
5. [Usage](#usage)
6. [Examples](#examples)
7. [Reference](#reference)

---

## Introduction

**EasyLog.Lib** is a C# library designed for event logging. It allows you to write logs to a file with customizable formatting through an extensible formatter system.

### Key Features

- Flexible formatting: Implement your own log format (JSON, CSV, plain text, etc.)
- Directory management: Automatic directory creation if necessary
- Daily rotation: Automatic log file rotation (one file per day)
- Dynamic directory: Change the log destination directory at any time

---

### Prerequisites
- .NET 10.0 or higher

---

## Architecture

### Main Components

#### 1. **EasyLog**
Class responsible for writing logs to a file. It uses a formatter to format the data before writing.

#### 2. **ILogFormatter** (interface)
Interface for formatting implementations. Any formatter must implement this interface.

#### 3. **JsonLogFormatter**
Default formatter that serializes logs to JSON format.

#### 4. **XmlLogFormatter**
Optional formatter that serializes logs to XML format.

---

## The 3 Logging Modes

EasyLog.Lib supports **3 logging modes** to adapt to different architectural needs:

### Mode 1: Direct Logging (Local File)

**Description:** Logs are written directly to local files.

**Usage:**
```csharp
ILogFormatter formatter = new JsonLogFormatter();
EasyLog logger = new EasyLog(formatter, "logs");

logger.Write(DateTime.Now, "backup", new Dictionary<string, object>
{
    { "status", "started" },
    { "fileCount", 150 }
});
```

**Advantages:**
- ✅ Simple and performant (no network)
- ✅ No external dependencies
- ✅ Full control over files

**Disadvantages:**
- ❌ Logs limited to local machine
- ❌ Manual centralization management

**Use cases:**
- Monolithic applications
- Local development
- Non-critical logs

---

### Mode 2: Network Client Logging

**Description:** Logs are sent via TCP to a remote EasyLog.Server which records them.

**Usage:**
```csharp
var client = new EasyLogNetworkClient("192.168.1.50", 5000);
client.Connect();

client.Send(DateTime.Now, "backup", new Dictionary<string, object>
{
    { "status", "started" },
    { "fileCount", 150 }
});

client.Disconnect();
```

**Advantages:**
- ✅ Centralized logs on a single server
- ✅ Distributed logs from multiple clients
- ✅ Scalable and flexible

**Disadvantages:**
- ❌ Depends on remote server
- ❌ Potential loss on disconnection
- ❌ Slight network overhead

**Use cases:**
- Distributed architectures
- Microservices
- Centralized logs from multiple machines

---

### Mode 3: Combined

**Description:** Using an EasyLog.Server along with local writing

---

## Comparison of Modes

| Aspect | Mode 1 (Direct) | Mode 2 (Server) | Mode 3 (Combined) |
|--------|-----------------|-----------------|------------------|
| **Storage** | Local | Server | Combined |
| **Performance** | Very fast | Fast | Fast |

---

## Usage

### Basic Initialization

```csharp
// Create a formatter (JSON in this example)
ILogFormatter formatter = new JsonLogFormatter();

// Initialize EasyLog with the formatter and log directory
// Logs will be automatically rotated daily with files named: yyyy-MM-dd_logs.json
EasyLog logger = new EasyLog(formatter, "logs");

// Write a log
var content = new Dictionary<string, object>
{
    { "action", "login" },
    { "userId", 123 },
    { "duration_ms", 456 }
};

logger.Write(DateTime.Now, "user_event", content);
```


---

## Examples

### Example 1: Logging User Events

```csharp
ILogFormatter formatter = new JsonLogFormatter();
EasyLog logger = new EasyLog(formatter, "logs/user_events");

// Login event
var loginEvent = new Dictionary<string, object>
{
    { "event_type", "login" },
    { "user_id", 42 },
    { "ip_address", "192.168.1.100" },
    { "success", true }
};

logger.Write(DateTime.Now, "auth", loginEvent);
```

**Output in file** (e.g., `logs/user_events/2025-02-05_logs.json`):
```json
{"logs":[{"timestamp":"2025-02-05 14:30:45","name":"auth","content":{"event_type":"login","user_id":42,"ip_address":"192.168.1.100","success":true}}]}
```

**Daily rotation**: Each day, a new file is created automatically (e.g., `2025-02-06_logs.json` the next day).

---

### Example 2: UNC Path Normalization

```csharp
ILogFormatter formatter = new JsonLogFormatter();
EasyLog logger = new EasyLog(formatter, "logs");

// Backup event with local paths
var backupEvent = new Dictionary<string, object>
{
    { "sourcePath", "C:\\Documents\\Data" },
    { "destinationPath", "D:\\Backups\\Data" },
    { "status", "completed" }
};

logger.Write(DateTime.Now, "backup", backupEvent);
```

**Output in file**:
```json
{"logs":[{"timestamp":"2025-02-05 14:30:45","name":"backup","content":{"sourcePath":"\\\\localhost\\C$\\Documents\\Data","destinationPath":"\\\\localhost\\D$\\Backups\\Data","status":"completed"}}]}
```

**Note:** Local paths are automatically converted to UNC format to ensure network compatibility and centralization.

---

## Reference

### EasyLog Class

#### Constructor
```csharp
public EasyLog(ILogFormatter formatter, string logDirectory)
```

**Parameters:**
- `formatter` (ILogFormatter): Formatter implementation to format the logs
- `logDirectory` (string): Path to the log directory. Logs are stored as daily files with format `yyyy-MM-dd_logs.json`

#### Write Method
```csharp
public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
```

**Description:** Writes a formatted log to the file. Any field containing the keyword "Path" in its key will be automatically normalized to UNC format.

**Parameters:**
- `timestamp` (DateTime): Event timestamp
- `name` (string): Event name or category
- `content` (Dictionary<string, object>): Event data

**UNC Normalization:** Local paths (ex: `C:\Users\file.txt`) are automatically converted to UNC paths (ex: `\\localhost\C$\Users\file.txt`)

#### SetLogPath Method
```csharp
public void SetLogPath(string newLogDirectory)
```

**Description:** Changes the log destination directory. The current log file is closed before switching directories.

**Parameters:**
- `newLogDirectory` (string): New log directory path

#### GetCurrentLogPath Method
```csharp
public string GetCurrentLogPath()
```

**Description:** Returns the currently used log file path (includes the date-specific filename).

**Return:** The full path to the current log file (string)

#### GetLogDirectory Method
```csharp
public string GetLogDirectory()
```

**Description:** Returns the log directory path currently in use.

**Return:** The log directory path (string)

#### Close Method
```csharp
public void Close()
```

**Description:** Closes the current log file using the formatter. This is useful when you want to finalize the log file before ending the application.

---

### ILogFormatter Interface

```csharp
public interface ILogFormatter
{
    string Format(DateTime timestamp, string name, Dictionary<string, object> content);
}
```

**Description:** Defines the contract for formatting implementations.

**Parameters:**
- `timestamp` (DateTime): Event timestamp
- `name` (string): Event name
- `content` (Dictionary<string, object>): Event content

**Return:** A formatted string ready to be written to the file

---

### JsonLogFormatter Class

```csharp
public class JsonLogFormatter : ILogFormatter
```

**Description:** Formatter that serializes logs to compact JSON format (without indentation).

**Output format:**
```json
{"timestamp":"yyyy-MM-dd HH:mm:ss","name":"event_name","content":{...}}
```

### XmlLogFormatter Class

```csharp
public class XmlLogFormatter : ILogFormatter
```

**Description:** Formatter that serializes logs to XML format with hierarchical structure.

**Output format:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<logs>
  <logEntry>
    <timestamp>2025-02-05 14:30:45</timestamp>
    <name>event_name</name>
    <content>...</content>
  </logEntry>
</logs>
```

---
