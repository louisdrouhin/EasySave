# EasySave - Technical Support Guide

## Overview

This document provides technical support teams with all necessary information to assist EasySave users, diagnose issues, and maintain the application.

---

## Minimum System Requirements

| Component | Requirement |
|---|---|
| Operating System | Windows 10 / 11 (64-bit) |
| Runtime | .NET 10 or higher |
| RAM | 2 GB minimum |
| Disk Space | 500 MB for installation + space for logs and backups |
| Permissions | Local administrator rights recommended |

The software also works on Linux/macOS.

---

## Default Locations

### Installation Procedure
1. Download EasySave from the official website or your administrator
2. Extract files to an installation folder
3. No additional installation is required, the application is ready to use

### Installation Folder
```
C:\Program Files\EasySave\
or
C:\Users\[User]\AppData\Local\EasySave\
```

### Directory Structure
```
EasySave/
├── CLI
    ├── logs/                     # Default logs folder
    ├── EasySave.Cli.exe          # Command-line executable
    ├── config.json               # Configuration file
    ├── state.json                # Real-time state file
    ├── EasyLog.Lib.dll           # Logger library
    └── Cryptosoft.exe            # Encryption software
└── GUI/
    ├── logs/                     # Default logs folder
    ├── EasySave.Gui.exe          # GUI executable
    ├── config.json               # Configuration file
    ├── state.json                # Real-time state file
    ├── EasyLog.Lib.dll           # Logger library
    └── Cryptosoft.exe            # Encryption software
```

---

## Configuration Files

### 1. **config.json** (to be updated)

**Location** : Installation root directory

**Structure** :
```json
{
  "config": {
    "logsPath": "./logs/",                      // Logs path
    "stateFilePath": "./state.json",            // State file
    "cryptosoftPath": "./Cryptosoft.exe",       // Encryption module path
    "logFormat": "json",                        // Logs format: "json" or "xml"
    "maxConcurrentJobs": 10,                    // Max parallel jobs (perf issue)
    "encryption": {                             // File extensions to encrypt
      "extensions": [".pdf", ".docx"]
    },
    "priorityExtensions": [".pdf", ".docx"],    // File extensions to prioritize
    "businessApplications": ["notepad"],        // Business apps to detect
    "largeFileSizeLimitKb": 10240               // Max file size in parallel
  },
  "easyLogServer": {                            // Centralized log server
    "enabled": false,
    "mode": "local_only",                       // "local_only", "server_only", "both"
    "host": "localhost",
    "port": 5000
  },
  "jobs": [
    {
      "name": "test",
      "type": "Full",
      "sourceDir": "/path/source",
      "targetDir": "/path/destination"
    }
  ]
}
```

### 2. **state.json** (Real-Time)

**Location** : Root directory (defined in config.json)

**Content** :
```json
{
  "jobs": [
    {
      "id": 1,
      "name": "Job 1",
      "state": "RUNNING",                    // PENDING, RUNNING, PAUSED, FINISHED, ERROR
      "progress": 45.5,                      // Progress percentage
      "filesProcessed": 150,
      "totalFiles": 330,
      "totalSize": 2048,                     // In MB
      "currentSpeed": 15.5,                  // MB/s
      "timeElapsed": 120,                    // Seconds
      "estimatedTimeRemaining": 90
    }
  ]
}
```

**Purpose** : Allows external processes to track real-time status

---

## Log Files

### Location
```
[Installation]/logs/YYYY-MM-DD_logs.json
[Installation]/logs/YYYY-MM-DD_logs.xml
```

### JSON Format (Standard)
```json
{
      "timestamp": "2026-02-24 09:20:19",
      "name": "ConfigParserInitialized",
      "content": {
        "configPath": "\\\\localhost\\C$\\\\Users\\Noiret\\Documents\\2 - Ressources\\3.2 - Etudes\\B2-genie_logiciel\\0-PROJET\\EasySave\\publish\\config.json"
      }
    },
```
The content part may vary depending on the nature of the log to provide all important information relative to the context.

**Rotation** : A new file is created each day

---

## Troubleshooting Procedures

### Error "Invalid source/destination path"

**Cause** : The path does not exist or is not accessible

**Solution** :
- Verify that the paths exist
- Ensure the user has read permissions (source) and write permissions (destination)
- For UNC: `\\server\share` → Verify network connectivity
```bash
# Test path access
dir "C:\path\source"
```

### Files Not Encrypted

**Check** :
- The extension is in the `encryption.extensions` list in config.json
- The `Cryptosoft.exe` file exists and is executable
- Permissions allow execution
---

## Important Files for Support

| File | Purpose |
|---------|---------|
| `config.json` | Configuration - **ALWAYS request in case of problems** |
| `logs/YYYY-MM-DD.json` | Today's logs - **Essential for diagnosis** |
| `state.json` | Current state - To check progress |
| `EasySave.exe` or `.Gui.exe` | Executable versions |

---

## Updates and Maintenance

### Configuration Backup
Before any update, backup:
- `config.json`
- Entire `logs/` folder
- `state.json`

---
## Appendices
### EasyLog
- [easylog.lib - French documentation](EasyLog.Lib/fr-docs.md)
- [easylog.lib - English documentation](EasyLog.Lib/en-docs.md)

- [easylog.server - French documentation](EasyLog.Server/fr-docs.md)
- [easylog.server - English documentation](EasyLog.Server/en-docs.md)

### Modeling
- [UML diagrams](Modelisation.md)

---

**Last updated** : February 2026
