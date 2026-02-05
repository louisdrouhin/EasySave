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
- Dynamic path: Change the log destination file at any time

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

// Initialize EasyLog with the formatter and log file path
EasyLog logger = new EasyLog(formatter, "logs/application.log");

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
EasyLog logger = new EasyLog(formatter, "logs/user_events.log");

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

**Output in file**:
```json
{"timestamp":"2025-02-05 14:30:45","name":"auth","content":{"event_type":"login","user_id":42,"ip_address":"192.168.1.100","success":true}}
```

---

## Reference

### EasyLog Class

#### Constructor
```csharp
public EasyLog(ILogFormatter formatter, string logPath)
```

**Parameters:**
- `formatter` (ILogFormatter): Formatter implementation to format the logs
- `logPath` (string): Path to the log file (created if it doesn't exist)

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
public void SetLogPath(string newLogPath)
```

**Description:** Changes the log destination file.

**Parameters:**
- `newLogPath` (string): New log file path

#### GetCurrentLogPath Method
```csharp
public string GetCurrentLogPath()
```

**Description:** Returns the currently used log file path.

**Return:** The log file path (string)

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

## Version
- v1.0.0 - Initial version with JSON format
- Target Framework: .NET 10.0
