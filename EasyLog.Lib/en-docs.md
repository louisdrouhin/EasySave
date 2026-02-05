# EasyLog.Lib - Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Architecture](#architecture)
4. [Usage](#usage)
5. [Examples](#examples)
6. [Reference](#reference)
7. [Custom Formatters](#custom-formatters)

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

**Description:** Writes a formatted log to the file.

**Parameters:**
- `timestamp` (DateTime): Event timestamp
- `name` (string): Event name or category
- `content` (Dictionary<string, object>): Event data

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

#### CloseJsonStructure Method
```csharp
public void CloseJsonStructure()
```

**Description:** Closes the JSON structure of the current log file by appending `]}`. This is useful when you want to finalize the log file before switching directories or ending the application.

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

---
