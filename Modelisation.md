# Modélisation UML - EasySave

Ce document regroupe les diagrammes UML du projet **EasySave**, réalisés avec [Mermaid](https://mermaid.js.org/).

## 1. Diagramme de Classes

Vue d'ensemble des principales classes du projet et de leurs relations.

```mermaid
classDiagram
  CLI --> JobManager

  JobManager *-- "0..5" Job
  JobManager o-- EasyLog
  JobManager o-- StateTracker

  JobManager --> ConfigParser
  ConfigParser ..> Job

  Job --> SaveType
  StateEntry --> StateType

  EasyLog o-- ILogFormatter
  ILogFormatter <|.. JsonLogFormatter

  JobManager ..> LogEntry
  JobManager ..> StateEntry
  StateTracker ..> StateEntry

  namespace EasySave.CLI {
	  class CLI {
		-JobManager : JobManager
	    +Start() void
	    +WriteLine(message: string) void
	  }
	}

	namespace EasySave.Core {
	  class JobManager {
	    -List~Job~ Jobs
	    -EasyLog Logger
	    -StateTracker StateTracker
	    +createLogger() void
	    +createJob(name: string, type: SaveType, sourceDir: string, targetDir: string) void
	    +deleteJob(id: int) void
	    +launchJob(id: int) void
	  }

      class StateTracker {
	    -string StatePath
	    +addOrEditJobState(state: StateEntry) void
	  }

      class ConfigParser {
        -string _configPath
        -JSON Config
        +LoadConfig() void
        +EditAndSaveConfig(newConfig: JSON) void
        +saveJobs(jobs: List<Job>) void
      }
  }

  namespace EasySave.Models {
	  class Job {
	    -string Name
	    -SaveType Type
	    -string SourceDir
	    -string TargetDir
	  }
	  class SaveType {
	    <<enumeration>>
	    Full
	    Differential
	  }
	  class LogEntry {
		  -string Name
		  -Datetime Timestamp
		  -string SourceFile
		  -string TargetFile
		  -long SizeFile
		  -long CopyTime
		  +ToString() string
	  }
	  class StateEntry {
		  -string Name
          -StateType State
          -long? TotalSize = null
          -float? Progress = null
          -long? RemainingSize = null
          +ToString() string?
	  }
      class StateType {
	    <<enumeration>>
	    Active
	    Inactive
	  }
  }

  namespace EasyLog.lib {
	  class EasyLog {
        -ILogFormatter _formatter
        -string _logPath
        +Write(timestamp: DateTime, name: string, content: Dictionary<string, object>) void
        +SetLogPath(newLogPath string) void
        +GetCurrentLogPath() string
        -EnsureDirectoryExists(logPath string) void
	  }

      class ILogFormatter {
        +Format(timestamp: DateTime, name: string, content: Dictionary<string, object>) string
      }

      class JsonLogFormatter {
      }
  }
```

## 2. Diagrammes de Séquence

### 2.1 Ajout d'un Job

```mermaid
sequenceDiagram
    actor User as User
    participant CLI as EasySave.CLI.CLI
    participant JobMgr as EasySave.Core.JobManager
    participant Config as EasySave.Core.ConfigParser
    participant Job as EasySave.Models.Job

    User->>CLI: Add Job
    CLI->>User: prompt("Name")
    User->>CLI: Name
    CLI->>User: prompt("Type")
    User->>CLI: Type
    CLI->>User: prompt("Source directory")
    User->>CLI: Source directory"
    CLI->>User: prompt("Target directory")
    User->>CLI: Target directory
    CLI->>JobMgr: +createJob(name: string, type: SaveType, sourceDir: string, targetDir: string)
    JobMgr->>Job: new() Job(name: string, type: SaveType, sourceDir: string, targetDir: string)
    Job->>Job:Create instance
    Job->>JobMgr: Return instance
    JobMgr->>JobMgr: Jobs.add(job)
    JobMgr->>Config: Write job to file
    Config->>JobMgr: Job written in the file
    JobMgr->>CLI: Job created
    CLI->>User: Job created
```

### 2.2 Lancement d'un Job

```mermaid
sequenceDiagram
    actor User as User
    participant CLI as EasySave.CLI.CLI
    participant JobMgr as EasySave.Core.JobManager
    participant State as EasySave.Core.StateTracker
    participant Log as EasyLog.Lib.Logger

    User->>CLI: Start Job
    CLI->>User: prompt("Id")
    User->>CLI: Id
    CLI->>JobMgr: +launchJob(id: int)

    JobMgr->>State: setState(ACTIVE)

    alt SaveType == Full
        loop for each source file
            JobMgr->>JobMgr: copyFile(file)
            JobMgr->>State: updateProgress(file)
            JobMgr->>Log: writeLog(fileInfo)
        end
    else SaveType == Differential
        loop for each source file
            JobMgr->>JobMgr: computeHash(file)
            JobMgr->>JobMgr: copyFile(file) if different
            JobMgr->>State: updateProgress(file)
            JobMgr->>Log: writeLog(fileInfo)
        end
    end

    JobMgr->>State: setState(INACTIVE)
    JobMgr-->>CLI: Job completed
    CLI-->>User: Job completed
```

### 2.3 Suppression d'un Job

```mermaid
sequenceDiagram
    actor User as User
    participant CLI as EasySave.CLI.CLI
    participant JobMgr as EasySave.Core.JobManager
    participant Config as EasySave.Core.ConfigParser

    User->>CLI: Delete Job
    CLI->>User: prompt("Id")
    User->>CLI: Id
    CLI->>JobMgr: +deleteJob(id: int)
    JobMgr->>JobMgr: Jobs.removeAt(Id)
    JobMgr->>Config: Remove job from the file
    Config->>JobMgr: Job removed from the file
    JobMgr->>CLI: Job deleted
    CLI->>User: Job deleted
```

### 2.4 Logger

```mermaid
sequenceDiagram
    participant App as JobManager
    participant Formatter as JsonLogFormatter
    participant Logger as EasyLog
    participant FS as File System

    Note over App,FS: Initialization
    App->>Formatter: new JsonLogFormatter()
    Formatter-->>App: formatter instance

    App->>Logger: new EasyLog(formatter, logDirectory)
    activate Logger
    Logger->>Logger: _logDirectory = logDirectory
    Logger->>Logger: _logPath = "logDirectory/2026-02-05_logs.json"
    Logger->>FS: EnsureDirectoryExists()
    Logger->>FS: WriteAllText(_logPath, "{"logs":[")
    deactivate Logger
    Logger-->>App: logger instance

    Note over App,FS: Log writing
    App->>Logger: Write(timestamp, "Event", content)
    activate Logger

    Logger->>Formatter: Format(timestamp, "Event", content)
    activate Formatter
    Formatter->>Formatter: Serialize to JSON
    Formatter-->>Logger: JSON string
    deactivate Formatter

    alt _isFirstEntry == true
        Logger->>FS: AppendAllText(path, jsonLog)
        Logger->>Logger: _isFirstEntry = false
    else _isFirstEntry == false
        Logger->>FS: AppendAllText(path, "," + jsonLog)
    end

    deactivate Logger
```
