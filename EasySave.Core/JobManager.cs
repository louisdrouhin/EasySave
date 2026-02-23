namespace EasySave.Core;

using EasySave.Models;
using EasyLog.Lib;
using EasySave.Core.Localization;
using System.Text.Json.Nodes;
using System.Diagnostics;

public class LogFormatChangedEventArgs : EventArgs
{
    public string OldFormat { get; set; }
    public string NewFormat { get; set; }

    public LogFormatChangedEventArgs(string oldFormat, string newFormat)
    {
        OldFormat = oldFormat;
        NewFormat = newFormat;
    }
}

public class JobManager
{
    private readonly List<Job> _jobs;
    private EasyLog _logger;
    private readonly ConfigParser _configParser;
    private ILogFormatter _logFormatter;
    private readonly StateTracker _stateTracker;
    private EasyLogNetworkClient? _networkClient;
    private string _logMode = "local_only";

    // Job control mechanisms
    private readonly Dictionary<string, CancellationTokenSource> _jobCancellationTokens = new();
    private readonly Dictionary<string, ManualResetEventSlim> _jobPauseEvents = new();

    // Business application monitoring
    private Thread? _businessAppMonitorThread;
    private CancellationTokenSource? _monitorCancellationTokenSource;
    private volatile bool _isBusinessAppRunning = false;
    private volatile string? _detectedBusinessApp = null;
    private readonly object _businessAppLock = new object();
    private readonly HashSet<string> _jobsPausedByBusinessApp = new();

    // Priority files management
    private int _priorityFilesPending = 0;
    private int _jobsWaitingToScan = 0;
    private readonly object _priorityLock = new object();
    private readonly ManualResetEventSlim _priorityWaitHandle = new ManualResetEventSlim(true);

    public ConfigParser ConfigParser => _configParser;

    public event EventHandler<LogFormatChangedEventArgs>? LogFormatChanged;

    public JobManager()
    {
        _configParser = new ConfigParser("config.json");
        _logFormatter = CreateLogFormatter();
        _logger = new EasyLog(_logFormatter, _configParser.GetLogsPath());

        try
        {
            InitializeNetworkClient();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JobManager] Network initialization error: {ex.Message}");
            _logMode = "local_only";
        }

        LogEvent(
            DateTime.Now,
            "ConfigParserInitialized",
            new Dictionary<string, object>
            {
                { "configPath", "../config.json" }
            }
        );

        LogEvent(
            DateTime.Now,
            "LoggerInitialized",
            new Dictionary<string, object>
            {
                { "formatterType", _logFormatter.GetType().Name },
                { "logsPath", _configParser.Config?["config"]?["logsPath"]?.GetValue<string>() ?? "logs.json" },
                { "logMode", _logMode }
            }
        );

        _jobs = new List<Job>();

        LogEvent(
            DateTime.Now,
            "JobsListCreated",
            new Dictionary<string, object>()
        );

        LoadJobsFromConfig();

        LogEvent(
            DateTime.Now,
            "JobsLoadedFromConfig",
            new Dictionary<string, object>
            {
                { "jobsCount", _jobs.Count }
            }
        );

        string stateFilePath = _configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>() ?? "state.json";
        _stateTracker = new StateTracker(stateFilePath);

        foreach (var job in _jobs)
        {
            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Inactive
                )
            );
        }

        LogEvent(
            DateTime.Now,
            "StateTrackerCreated",
            new Dictionary<string, object>()
        );

        StartBusinessAppMonitoring();
    }

    private void InitializeNetworkClient()
    {
        var serverConfig = _configParser.Config?["easyLogServer"];
        bool isEnabled = serverConfig?["enabled"]?.GetValue<bool>() ?? false;

        if (!isEnabled)
        {
            _logMode = "local_only";
            return;
        }

        _logMode = serverConfig?["mode"]?.GetValue<string>()?.ToLower() ?? "local_only";

        if (_logMode == "local_only")
            return;

        string host = serverConfig?["host"]?.GetValue<string>() ?? "localhost";
        int port = serverConfig?["port"]?.GetValue<int>() ?? 5000;

        try
        {
            _networkClient = new EasyLogNetworkClient(host, port);
            _networkClient.Connect();
            Console.WriteLine($"[JobManager] Connected to EasyLog server at {host}:{port}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JobManager] Failed to connect to EasyLog server: {ex.Message}");
            _logMode = "local_only";
            _networkClient = null;
        }
    }

    private void LogEvent(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        content["clientId"] = Environment.MachineName;

        switch (_logMode)
        {
            case "local_only":
                _logger.Write(timestamp, name, content);
                break;

            case "server_only":
                if (_networkClient != null && _networkClient.IsConnected)
                {
                    try
                    {
                        _networkClient.Send(timestamp, name, content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[JobManager] Error sending log to server: {ex.Message}");
                    }
                }
                break;

            case "both":
                _logger.Write(timestamp, name, content);
                if (_networkClient != null && _networkClient.IsConnected)
                {
                    try
                    {
                        _networkClient.Send(timestamp, name, content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[JobManager] Error sending log to server: {ex.Message}");
                    }
                }
                break;
        }
    }

    private ILogFormatter CreateLogFormatter()
    {
        string logFormat = _configParser.Config?["config"]?["logFormat"]?.GetValue<string>()?.ToLower() ?? "json";

        return logFormat switch
        {
            "xml" => new XmlLogFormatter(),
            "json" => new JsonLogFormatter(),
            _ => new JsonLogFormatter()
        };
    }

    public void CreateJob(string name, JobType type, string sourcePath, string destinationPath)
    {
        var job = new Job(name, type, sourcePath, destinationPath);

        _jobs.Add(job);

        SaveJobToConfig(job);

        _stateTracker.UpdateJobState(
            new StateEntry(
              job.Name,
              DateTime.Now,
              JobState.Inactive
            )
        );

        LogEvent(
            DateTime.Now,
            "JobCreated",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "jobType", job.Type.ToString() },
                { "sourcePath", job.SourcePath },
                { "destinationPath", job.DestinationPath }
            }
        );
    }

    public void removeJob(int index)
    {
        var jobToRemove = _jobs.ElementAtOrDefault(index);

        if (jobToRemove == null)
        {
            return;
        }

        _jobs.Remove(jobToRemove);

        RemoveJobFromConfig(index);

        _stateTracker.RemoveJobState(index);

        LogEvent(
            DateTime.Now,
            "JobDeleted",
            new Dictionary<string, object>
            {
                { "jobName", jobToRemove.Name },
                { "jobType", jobToRemove.Type.ToString() },
                { "sourcePath", jobToRemove.SourcePath },
                { "destinationPath", jobToRemove.DestinationPath }
            }
        );
    }

    public List<Job> GetJobs()
    {
        return _jobs;
    }

    public void Close()
    {
        StopBusinessAppMonitoring();

        _logger.Close();
        _networkClient?.Disconnect();
    }

    public string? CheckBusinessApplications()
    {
        var businessApplications = _configParser.GetBusinessApplications();

        foreach (var appName in businessApplications)
        {
            try
            {
                var processes = Process.GetProcessesByName(appName);
                if (processes.Length > 0)
                {
                    LogEvent(
                        DateTime.Now,
                        "BusinessApplicationDetected",
                        new Dictionary<string, object>
                        {
                            { "applicationName", appName },
                            { "message", $"Business application '{appName}' is running. Backup job execution blocked." }
                        }
                    );

                    return appName;
                }
            }
            catch (Exception ex)
            {
                LogEvent(
                    DateTime.Now,
                    "BusinessApplicationCheckError",
                    new Dictionary<string, object>
                    {
                        { "applicationName", appName },
                        { "error", ex.Message }
                    }
                );
            }
        }

        return null;
    }

    public void SetLogFormat(string format)
    {
        string oldFormat = _configParser.GetLogFormat();

        _logger.Close();
        _networkClient?.Disconnect();

        _configParser.SetLogFormat(format);

        _configParser.LoadConfig();

        _logFormatter = CreateLogFormatter();

        _logger = new EasyLog(_logFormatter, _configParser.GetLogsPath());

        InitializeNetworkClient();

        LogEvent(
            DateTime.Now,
            "LogFormatChanged",
            new Dictionary<string, object>
            {
                { "oldFormat", oldFormat },
                { "newFormat", format },
                { "newFormatterType", _logFormatter.GetType().Name }
            }
        );

        OnLogFormatChanged(oldFormat, format);
    }

    private void OnLogFormatChanged(string oldFormat, string newFormat)
    {
        LogFormatChanged?.Invoke(this, new LogFormatChangedEventArgs(oldFormat, newFormat));
    }

    public string GetLogFormat()
    {
        return _configParser.GetLogFormat();
    }

    private void LoadJobsFromConfig()
    {
        var jobsArray = _configParser.Config?["jobs"]?.AsArray();

        if (jobsArray == null || jobsArray.Count == 0)
        {
            return;
        }

        foreach (var jobNode in jobsArray)
        {
            if (jobNode == null) continue;

            var name = jobNode["name"]?.GetValue<string>() ?? string.Empty;
            var typeString = jobNode["type"]?.GetValue<string>() ?? "Full";
            var sourcePath = jobNode["sourceDir"]?.GetValue<string>() ?? string.Empty;
            var destinationPath = jobNode["targetDir"]?.GetValue<string>() ?? string.Empty;

            JobType jobType = typeString.ToLower() switch
            {
                "differential" => JobType.Differential,
                "full" => JobType.Full,
                _ => JobType.Full
            };

            var job = new Job(name, jobType, sourcePath, destinationPath);
            _jobs.Add(job);
        }
    }

    private void SaveJobToConfig(Job job)
    {
        var jobsArray = _configParser.Config?["jobs"]?.AsArray();

        if (jobsArray == null)
        {
            if (_configParser.Config is JsonObject configObject)
            {
                jobsArray = new JsonArray();
                configObject["jobs"] = jobsArray;
            }
            else
            {
                return;
            }
        }

        var newJobNode = new JsonObject
        {
            ["name"] = job.Name,
            ["type"] = job.Type.ToString(),
            ["sourceDir"] = job.SourcePath,
            ["targetDir"] = job.DestinationPath
        };

        jobsArray.Add(newJobNode);

        _configParser.EditAndSaveConfig(_configParser.Config!);
    }

    private void RemoveJobFromConfig(int index)
    {
        var jobsArray = _configParser.Config?["jobs"]?.AsArray();

        if (jobsArray == null)
        {
            return;
        }

        if (index >= 0 && index < jobsArray.Count)
        {
            jobsArray.RemoveAt(index);
            _configParser.EditAndSaveConfig(_configParser.Config!);
        }
    }

    public void PauseJob(Job job)
    {
        if (_jobPauseEvents.TryGetValue(job.Name, out var pauseEvent))
        {
            pauseEvent.Reset();

            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Paused
                )
            );

            LogEvent(
                DateTime.Now,
                "JobPaused",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name }
                }
            );
        }
    }

    public void ResumeJob(Job job)
    {
        lock (_businessAppLock)
        {
            if (_isBusinessAppRunning)
            {
                LogEvent(
                    DateTime.Now,
                    "JobResumeBlocked",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "reason", $"Business application '{_detectedBusinessApp}' is running" }
                    }
                );
                return;
            }
        }

        if (_jobPauseEvents.TryGetValue(job.Name, out var pauseEvent))
        {
            lock (_businessAppLock)
            {
                _jobsPausedByBusinessApp.Remove(job.Name);
            }

            pauseEvent.Set();

            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Active
                )
            );

            LogEvent(
                DateTime.Now,
                "JobResumed",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name }
                }
            );
        }
    }

    public void StopJob(Job job)
    {
        if (_jobCancellationTokens.TryGetValue(job.Name, out var cts))
        {
            cts.Cancel();

            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Inactive
                )
            );

            LogEvent(
                DateTime.Now,
                "JobStopped",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name }
                }
            );
        }
    }

    public void LaunchJob(Job job, string password, bool alreadyRegistered = false)
    {
        if (!alreadyRegistered)
        {
            RegisterJobForPriorityScan();
        }
        var cts = new CancellationTokenSource();
        var pauseEvent = new ManualResetEventSlim(true); 

        _jobCancellationTokens[job.Name] = cts;
        _jobPauseEvents[job.Name] = pauseEvent;

        _stateTracker.UpdateJobState(
            new StateEntry(
                job.Name,
                DateTime.Now,
                JobState.Active
              )
            );

        LogEvent(
            DateTime.Now,
            "JobStarted",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "jobType", job.Type.ToString() },
                { "sourcePath", job.SourcePath },
                { "destinationPath", job.DestinationPath }
            }
        );

        try
        {
            switch (job.Type)
            {
                case JobType.Full:
                    ExecuteFullBackup(job, password, cts.Token, pauseEvent);
                    break;
                case JobType.Differential:
                    ExecuteDifferentialBackup(job, password, cts.Token, pauseEvent);
                    break;
                default:
                    throw new InvalidOperationException($"Job type not supported : {job.Type}");
            }

            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Inactive
                  )
                );

            LogEvent(
                DateTime.Now,
                "JobCompleted",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "jobType", job.Type.ToString() }
                }
            );
        }
        catch (OperationCanceledException)
        {
            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Inactive
                )
            );

            LogEvent(
                DateTime.Now,
                "JobCancelled",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "jobType", job.Type.ToString() }
                }
            );
        }
        catch (Exception ex)
        {
            _stateTracker.UpdateJobState(
                new StateEntry(
                    job.Name,
                    DateTime.Now,
                    JobState.Inactive
                )
            );

            LogEvent(
                DateTime.Now,
                "JobFailed",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "jobType", job.Type.ToString() },
                    { "error", ex.Message }
                }
            );
            throw;
        }
        finally
        {
            _jobCancellationTokens.Remove(job.Name);
            if (_jobPauseEvents.Remove(job.Name, out var pauseEventToDispose))
            {
                pauseEventToDispose.Dispose();
            }
            cts.Dispose();
        }
    }

    private void ProcessDifferentialFile((string Path, string RelativePath, long Size, string Hash) fileToCopy, string diffBackupPath, Job job, string password, List<string> encryptExtensions, ref int filesProcessed, ref long totalBytesTransferred, int totalFilesToTransfer, long totalSizeToTransfer, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent)
    {
        cancellationToken.ThrowIfCancellationRequested();

        pauseEvent.Wait(cancellationToken);

        try
        {
            var destinationFile = Path.Combine(diffBackupPath, fileToCopy.RelativePath);
            var destinationDir = Path.GetDirectoryName(destinationFile);

            if (destinationDir != null && !Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            long encryptResult = CopyOrEncryptFile(fileToCopy.Path, destinationFile, password, encryptExtensions);

            filesProcessed++;
            totalBytesTransferred += fileToCopy.Size;

            _stateTracker.UpdateJobState(new StateEntry(
                job.Name,
                DateTime.Now,
                JobState.Active,
                totalFilesToTransfer,
                totalSizeToTransfer,
                (double)filesProcessed / totalFilesToTransfer * 100,
                totalFilesToTransfer - filesProcessed,
                totalSizeToTransfer - totalBytesTransferred,
                fileToCopy.Path,
                destinationFile
            ));

            bool hadError = encryptResult < 0;

            if (!hadError)
            {
                string logType = encryptResult > 0 ? "FileEncrypted" : "FileCopied";
                var logData = new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "sourceFile", fileToCopy.Path },
                    { "destinationFile", destinationFile },
                    { "fileSize", fileToCopy.Size },
                    { "operation", encryptResult > 0 ? "encryption" : "copy" },
                    { "encryptTimeMs", encryptResult > 0 ? encryptResult : 0L },
                    { "hash", fileToCopy.Hash }
                };

                LogEvent(
                    DateTime.Now,
                    logType,
                    logData
                );
            }
            else
            {
                var logData = new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "sourceFile", fileToCopy.Path },
                    { "destinationFile", destinationFile },
                    { "fileSize", fileToCopy.Size },
                    { "operation", "encryption" },
                    { "encryptTimeMs", encryptResult },
                    { "hash", fileToCopy.Hash }
                };

                LogEvent(
                    DateTime.Now,
                    "FileEncryptionError",
                    logData
                );
            }
        }
        catch (Exception ex)
        {
            LogEvent(
                DateTime.Now,
                "FileCopyError",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "sourceFile", fileToCopy.Path },
                    { "error", ex.Message }
                }
            );
        }
    }

    private void ProcessFile(string sourceFile, string backupPath, Job job, string password, List<string> encryptExtensions, bool createHashFile, Dictionary<string, string>? hashDictionary, ref int filesProcessed, ref long totalBytesTransferred, int totalFiles, long totalSize, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        // Wait if paused
        pauseEvent.Wait(cancellationToken);

        try
        {
            var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
            var destinationFile = Path.Combine(backupPath, relativePath);

            var destinationDir = Path.GetDirectoryName(destinationFile);
            if (destinationDir != null && !Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            var fileInfo = new FileInfo(sourceFile);
            var fileSize = fileInfo.Length;

            long encryptResult = CopyOrEncryptFile(sourceFile, destinationFile, password, encryptExtensions);

            if (createHashFile && hashDictionary != null)
            {
                try
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    using (var stream = File.OpenRead(sourceFile))
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        var fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        hashDictionary[relativePath] = fileHash;
                    }
                }
                catch (Exception hashEx)
                {
                    LogEvent(
                        DateTime.Now,
                        "HashCalculationError",
                        new Dictionary<string, object>
                        {
                            { "jobName", job.Name },
                            { "sourceFilePath", sourceFile },
                            { "error", hashEx.Message }
                        }
                    );
                }
            }

            filesProcessed++;
            totalBytesTransferred += fileSize;

            _stateTracker.UpdateJobState(new StateEntry(
                job.Name,
                DateTime.Now,
                JobState.Active,
                totalFiles,
                totalSize,
                (double)filesProcessed / totalFiles * 100,
                totalFiles - filesProcessed,
                totalSize - totalBytesTransferred,
                sourceFile,
                destinationFile
            ));

            bool wasEncrypted = encryptResult > 0 || encryptResult == 0;
            bool hadError = encryptResult < 0;

            if (!hadError)
            {
                string logType = encryptResult > 0 ? "FileEncrypted" : "FileCopied";
                var logData = new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "sourceFilePath", sourceFile },
                    { "destinationFilePath", destinationFile },
                    { "fileSize", fileSize },
                    { "operation", encryptResult > 0 ? "encryption" : "copy" },
                    { "encryptTimeMs", encryptResult > 0 ? encryptResult : 0L },
                    { "progress", $"{filesProcessed}/{totalFiles}" }
                };

                LogEvent(
                    DateTime.Now,
                    logType,
                    logData
                );
            }
            else
            {
                var logData = new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "sourceFilePath", sourceFile },
                    { "destinationFilePath", destinationFile },
                    { "fileSize", fileSize },
                    { "operation", "encryption" },
                    { "encryptTimeMs", encryptResult },
                    { "progress", $"{filesProcessed}/{totalFiles}" }
                };

                LogEvent(
                    DateTime.Now,
                    "FileEncryptionError",
                    logData
                );
            }
        }
        catch (Exception ex)
        {
            LogEvent(
                DateTime.Now,
                "FileCopyError",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "sourceFilePath", sourceFile },
                    { "error", ex.Message }
                }
            );
        }
    }

    private void ExecuteFullBackup(Job job, string password, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent, bool createHashFile = false)
    {
        if (!Directory.Exists(job.SourcePath))
        {
            throw new DirectoryNotFoundException($"The source directory does not exist : {job.SourcePath}");
        }

        if (!Directory.Exists(job.DestinationPath))
        {
            Directory.CreateDirectory(job.DestinationPath);
        }

        var encryptExtensions = _configParser.GetEncryptionExtensions();
        var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        var backupFolderName = $"FULL_{timestamp}";
        var fullBackupPath = Path.Combine(job.DestinationPath, backupFolderName);

        Directory.CreateDirectory(fullBackupPath);

        LogEvent(
            DateTime.Now,
            "BackupFolderCreated",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "backupPath", fullBackupPath },
                { "timestamp", timestamp }
            }
        );

        var sourceFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);
        int totalFiles = sourceFiles.Length;
        long totalSize = sourceFiles.Select(f => new FileInfo(f).Length).Sum();
        int filesProcessed = 0;
        long totalBytesTransferred = 0;
        var hashDictionary = createHashFile ? new Dictionary<string, string>() : null;

        _stateTracker.UpdateJobState(new StateEntry(
            job.Name,
            DateTime.Now,
            JobState.Active,
            totalFiles,
            totalSize,
            0,
            totalFiles,
            totalSize,
            "",
            ""
        ));

        LogEvent(
            DateTime.Now,
            "FullBackupStarted",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "totalFiles", totalFiles },
                { "backupFolder", backupFolderName },
                { "createHashFile", createHashFile }
            }
        );

        var priorityExtensions = _configParser.GetPriorityExtensions();
        var priorityFiles = new List<string>();
        var nonPriorityFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var extension = Path.GetExtension(sourceFile).ToLower();
            if (priorityExtensions.Contains(extension))
            {
                priorityFiles.Add(sourceFile);
            }
            else
            {
                nonPriorityFiles.Add(sourceFile);
            }
        }

        ReportPriorityFilesFound(priorityFiles.Count);

        // Process priority files
        foreach (var sourceFile in priorityFiles)
        {
            ProcessFile(sourceFile, fullBackupPath, job, password, encryptExtensions, createHashFile, hashDictionary, ref filesProcessed, ref totalBytesTransferred, totalFiles, totalSize, cancellationToken, pauseEvent);
            ReportPriorityFileProcessed();
        }

        // Wait for all priority files in all jobs to be processed
        _priorityWaitHandle.Wait(cancellationToken);

        // Process non-priority files
        foreach (var sourceFile in nonPriorityFiles)
        {
            ProcessFile(sourceFile, fullBackupPath, job, password, encryptExtensions, createHashFile, hashDictionary, ref filesProcessed, ref totalBytesTransferred, totalFiles, totalSize, cancellationToken, pauseEvent);
        }

        if (createHashFile && hashDictionary != null)
        {
            var hashFilePath = Path.Combine(job.DestinationPath, "hash.json");
            try
            {
                var hashJson = System.Text.Json.JsonSerializer.Serialize(hashDictionary, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(hashFilePath, hashJson);

                LogEvent(
                    DateTime.Now,
                    "HashFileCreated",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "hashFilePath", hashFilePath },
                        { "entriesCount", hashDictionary.Count }
                    }
                );
            }
            catch (Exception ex)
            {
                LogEvent(
                    DateTime.Now,
                    "HashFileCreationError",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "error", ex.Message }
                    }
                );
            }
        }

        LogEvent(
            DateTime.Now,
            "FullBackupCompleted",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "filesProcessed", filesProcessed },
                { "totalFiles", totalFiles },
                { "totalBytesTransferred", totalBytesTransferred },
                { "backupFolder", backupFolderName }
            }
        );
    }

    private void ExecuteDifferentialBackup(Job job, string password, CancellationToken cancellationToken, ManualResetEventSlim pauseEvent)
    {

        /// TO DO : check if modif else print message and no copy

        if (!Directory.Exists(job.SourcePath))
        {
            throw new DirectoryNotFoundException($"The source directory does not exist : {job.SourcePath}");
        }

        if (!Directory.Exists(job.DestinationPath))
        {
            Directory.CreateDirectory(job.DestinationPath);
        }

        var encryptExtensions = _configParser.GetEncryptionExtensions();
        var hashFilePath = Path.Combine(job.DestinationPath, "hash.json");
        if (!File.Exists(hashFilePath))
        {
            LogEvent(
                DateTime.Now,
                "NoHashFileFound",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "message", "No hash.json file found, switching to full backup" }
                }
            );

            ExecuteFullBackup(job, password, cancellationToken, pauseEvent, createHashFile: true);
            return;
        }

        Dictionary<string, string> hashDictionary;
        try
        {
            var hashJson = File.ReadAllText(hashFilePath);
            hashDictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(hashJson)
                ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading the hash.json file : {ex.Message}", ex);
        }

        var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        var backupFolderName = $"DIFF_{timestamp}";
        var diffBackupPath = Path.Combine(job.DestinationPath, backupFolderName);
        Directory.CreateDirectory(diffBackupPath);

        LogEvent(
            DateTime.Now,
            "DifferentialBackupStarted",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "backupFolder", backupFolderName },
                { "hashFileLoaded", hashFilePath }
            }
        );

        var allSourceFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);
        var filesToCopy = new List<(string Path, string RelativePath, long Size, string Hash)>();
        var newHashDictionary = new Dictionary<string, string>();
        long totalSizeToTransfer = 0;

        foreach (var sourceFile in allSourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            pauseEvent.Wait(cancellationToken);

            try
            {
                var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
                var fileInfo = new FileInfo(sourceFile);
                string currentHash;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                using (var stream = File.OpenRead(sourceFile))
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    currentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                newHashDictionary[relativePath] = currentHash;

                if (!hashDictionary.TryGetValue(relativePath, out var oldHash) || oldHash != currentHash)
                {
                    filesToCopy.Add((sourceFile, relativePath, fileInfo.Length, currentHash));
                    totalSizeToTransfer += fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                LogEvent(
                    DateTime.Now,
                    "FileProcessError",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "sourceFilePath", sourceFile },
                        { "error", ex.Message }
                    }
                );
            }
        }

        int totalFilesToTransfer = filesToCopy.Count;
        int filesProcessed = 0;
        long totalBytesTransferred = 0;

        _stateTracker.UpdateJobState(new StateEntry(
            job.Name,
            DateTime.Now,
            JobState.Active,
            totalFilesToTransfer,
            totalSizeToTransfer,
            0,
            totalFilesToTransfer,
            totalSizeToTransfer,
            "",
            ""
        ));

        var priorityExtensions = _configParser.GetPriorityExtensions();
        var priorityFilesToCopy = new List<(string Path, string RelativePath, long Size, string Hash)>();
        var nonPriorityFilesToCopy = new List<(string Path, string RelativePath, long Size, string Hash)>();

        foreach (var file in filesToCopy)
        {
            var extension = Path.GetExtension(file.Path).ToLower();
            if (priorityExtensions.Contains(extension))
            {
                priorityFilesToCopy.Add(file);
            }
            else
            {
                nonPriorityFilesToCopy.Add(file);
            }
        }

        ReportPriorityFilesFound(priorityFilesToCopy.Count);

        // Process priority files
        foreach (var fileToCopy in priorityFilesToCopy)
        {
            ProcessDifferentialFile(fileToCopy, diffBackupPath, job, password, encryptExtensions, ref filesProcessed, ref totalBytesTransferred, totalFilesToTransfer, totalSizeToTransfer, cancellationToken, pauseEvent);
            ReportPriorityFileProcessed();
        }

        // Wait for all priority files in all jobs to be processed
        _priorityWaitHandle.Wait(cancellationToken);

        // Process non-priority files
        foreach (var fileToCopy in nonPriorityFilesToCopy)
        {
            ProcessDifferentialFile(fileToCopy, diffBackupPath, job, password, encryptExtensions, ref filesProcessed, ref totalBytesTransferred, totalFilesToTransfer, totalSizeToTransfer, cancellationToken, pauseEvent);
        }

        try
        {
            var newHashJson = System.Text.Json.JsonSerializer.Serialize(newHashDictionary, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(hashFilePath, newHashJson);

            LogEvent(
                DateTime.Now,
                "HashFileUpdated",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "hashFilePath", hashFilePath },
                    { "entriesCount", newHashDictionary.Count }
                }
            );
        }
        catch (Exception ex)
        {
            LogEvent(
                DateTime.Now,
                "HashFileUpdateError",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "error", ex.Message }
                }
            );
        }

        LogEvent(
            DateTime.Now,
            "DifferentialBackupCompleted",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "totalFiles", allSourceFiles.Length },
                { "filesProcessed", allSourceFiles.Length },
                { "filesModified", totalFilesToTransfer },
                { "totalBytesTransferred", totalBytesTransferred },
                { "backupFolder", backupFolderName }
            }
        );
    }

    //================================================//
    //                CRYPTOSOFT UTILS                //
    //================================================//

    private long CopyOrEncryptFile(string sourceFile, string destinationFile, string password, List<string> encryptExtensions)
    {
        var fileExtension = Path.GetExtension(sourceFile).ToLower();

        if (encryptExtensions.Contains(fileExtension) && !string.IsNullOrEmpty(password))
        {
            var targetDirectory = Path.GetDirectoryName(destinationFile);
            if (targetDirectory == null)
            {
                throw new InvalidOperationException($"Unable to determine the target directory for : {destinationFile}");
            }
            return ExecuteCryptosoftCommand("-c", sourceFile, password, targetDirectory, "CryptosoftExecutionError");
        }
        else
        {
            File.Copy(sourceFile, destinationFile, overwrite: true);
            return 0;
        }
    }

    private long ExecuteCryptosoftCommand(string operation, string sourceFile, string password, string targetDirectory, string errorLogType)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {

            var appDirectory = AppContext.BaseDirectory;
            var cryptosoftPath = _configParser.Config?["config"]?["cryptosoftPath"]?.GetValue<string>() ?? "Cryptosoft.exe";

            // Resolve relative path if necessary
            if (!Path.IsPathRooted(cryptosoftPath))
            {
                cryptosoftPath = Path.Combine(appDirectory, cryptosoftPath);
            }

            if (!File.Exists(cryptosoftPath))
            {
                return -1;
            }

            var arguments = $"{operation} \"{sourceFile}\" \"{password}\" \"{targetDirectory}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = cryptosoftPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    return -2;
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    LogEvent(
                        DateTime.Now,
                        errorLogType,
                        new Dictionary<string, object>
                        {
                            { "sourceFile", sourceFile },
                            { "targetDirectory", targetDirectory },
                            { "exitCode", process.ExitCode },
                            { "error", error }
                        }
                    );
                    return -process.ExitCode;
                }
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            LogEvent(
                DateTime.Now,
                errorLogType,
                new Dictionary<string, object>
                {
                    { "sourceFile", sourceFile },
                    { "targetDirectory", targetDirectory },
                    { "error", ex.Message }
                }
            );
            return -999;
        }
    }

    //================================================//
    //     BUSINESS APPLICATION MONITORING            //
    //================================================//

    private void StartBusinessAppMonitoring()
    {
        _monitorCancellationTokenSource = new CancellationTokenSource();
        _businessAppMonitorThread = new Thread(() => MonitorBusinessApplications(_monitorCancellationTokenSource.Token))
        {
            IsBackground = true,
            Name = "BusinessAppMonitor"
        };
        _businessAppMonitorThread.Start();

        LogEvent(
            DateTime.Now,
            "BusinessAppMonitoringStarted",
            new Dictionary<string, object>
            {
                { "message", "Business application monitoring thread started" }
            }
        );
    }

    private void StopBusinessAppMonitoring()
    {
        if (_monitorCancellationTokenSource != null)
        {
            _monitorCancellationTokenSource.Cancel();
            _businessAppMonitorThread?.Join(TimeSpan.FromSeconds(2));
            _monitorCancellationTokenSource.Dispose();

            LogEvent(
                DateTime.Now,
                "BusinessAppMonitoringStopped",
                new Dictionary<string, object>
                {
                    { "message", "Business application monitoring thread stopped" }
                }
            );
        }
    }

    private void MonitorBusinessApplications(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var detectedApp = CheckBusinessApplications();

                lock (_businessAppLock)
                {
                    bool previousState = _isBusinessAppRunning;
                    _isBusinessAppRunning = detectedApp != null;
                    _detectedBusinessApp = detectedApp;

                    if (_isBusinessAppRunning && !previousState)
                    {
                        LogEvent(
                            DateTime.Now,
                            "BusinessAppDetected_AutoPause",
                            new Dictionary<string, object>
                            {
                                { "applicationName", _detectedBusinessApp! },
                                { "action", "Pausing all active jobs" }
                            }
                        );

                        PauseAllActiveJobs();
                    }
                    else if (!_isBusinessAppRunning && previousState)
                    {
                        LogEvent(
                            DateTime.Now,
                            "BusinessAppClosed_AutoResume",
                            new Dictionary<string, object>
                            {
                                { "applicationName", _detectedBusinessApp ?? "unknown" },
                                { "action", "Resuming paused jobs" }
                            }
                        );

                        ResumeJobsPausedByBusinessApp();
                    }
                }

                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                LogEvent(
                    DateTime.Now,
                    "BusinessAppMonitoringError",
                    new Dictionary<string, object>
                    {
                        { "error", ex.Message }
                    }
                );

                Thread.Sleep(5000);
            }
        }
    }

    private void PauseAllActiveJobs()
    {
        foreach (var kvp in _jobPauseEvents.ToList())
        {
            var jobName = kvp.Key;
            var pauseEvent = kvp.Value;

            if (pauseEvent.IsSet)
            {
                pauseEvent.Reset();
                _jobsPausedByBusinessApp.Add(jobName);

                _stateTracker.UpdateJobState(
                    new StateEntry(
                        jobName,
                        DateTime.Now,
                        JobState.Paused
                    )
                );

                LogEvent(
                    DateTime.Now,
                    "JobAutoPausedByBusinessApp",
                    new Dictionary<string, object>
                    {
                        { "jobName", jobName },
                        { "businessApp", _detectedBusinessApp! }
                    }
                );
            }
        }
    }

    private void ResumeJobsPausedByBusinessApp()
    {
        foreach (var jobName in _jobsPausedByBusinessApp.ToList())
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var pauseEvent))
            {
                pauseEvent.Set();

                _stateTracker.UpdateJobState(
                    new StateEntry(
                        jobName,
                        DateTime.Now,
                        JobState.Active
                    )
                );

                LogEvent(
                    DateTime.Now,
                    "JobAutoResumedAfterBusinessApp",
                    new Dictionary<string, object>
                    {
                        { "jobName", jobName }
                    }
                );
            }
        }

        _jobsPausedByBusinessApp.Clear();
    }

    public async Task LaunchMultipleJobsAsync(List<Job> jobs, string password)
    {
        lock (_businessAppLock)
        {
            if (_isBusinessAppRunning)
            {
                var message = $"Cannot start jobs: Business application '{_detectedBusinessApp}' is running.";
                LogEvent(
                    DateTime.Now,
                    "JobsLaunchBlocked",
                    new Dictionary<string, object>
                    {
                        { "reason", message },
                        { "businessApp", _detectedBusinessApp! }
                    }
                );
                throw new InvalidOperationException(message);
            }
        }

        var maxConcurrentJobs = _configParser.GetMaxConcurrentJobs();
        var semaphore = new SemaphoreSlim(maxConcurrentJobs, maxConcurrentJobs);
        var tasks = new List<Task>();

        foreach (var job in jobs)
        {
            RegisterJobForPriorityScan();
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    LaunchJob(job, password, true);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    //================================================//
    //          DECRYPTION METHODS                    //
    //================================================//

    public void DecryptBackup(string backupPath, string restorePath, string password)
    {
        if (!Directory.Exists(backupPath))
        {
            throw new DirectoryNotFoundException($"The backup path does not exist : {backupPath}");
        }

        if (!Directory.Exists(restorePath))
        {
            Directory.CreateDirectory(restorePath);
        }

        LogEvent(
            DateTime.Now,
            "DecryptBackupStarted",
            new Dictionary<string, object>
            {
                { "backupPath", backupPath },
                { "restorePath", restorePath }
            }
        );

        var encryptedFiles = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories);
        int totalFiles = encryptedFiles.Length;
        int filesProcessed = 0;
        long totalBytesTransferred = 0;

        foreach (var encryptedFile in encryptedFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(backupPath, encryptedFile);
                var restoreFile = Path.Combine(restorePath, relativePath);
                var restoreDir = Path.GetDirectoryName(restoreFile);

                if (restoreDir == null)
                {
                    throw new InvalidOperationException($"Unable to determine the restore directory for : {restoreFile}");
                }

                if (!Directory.Exists(restoreDir))
                {
                    Directory.CreateDirectory(restoreDir);
                }

                ExecuteCryptosoftCommand("-d", encryptedFile, password, restoreDir, "CryptosoftDecryptionError");

                var fileInfo = new FileInfo(encryptedFile);
                filesProcessed++;
                totalBytesTransferred += fileInfo.Length;

                LogEvent(
                    DateTime.Now,
                    "FileDecrypted",
                    new Dictionary<string, object>
                    {
                        { "encryptedFile", encryptedFile },
                        { "restoreFile", restoreFile },
                        { "progress", $"{filesProcessed}/{totalFiles}" }
                    }
                );
            }
            catch (Exception ex)
            {
                LogEvent(
                    DateTime.Now,
                    "FileDecryptError",
                    new Dictionary<string, object>
                    {
                        { "encryptedFile", encryptedFile },
                        { "error", ex.Message }
                    }
                );
                throw;
            }
        }

        LogEvent(
            DateTime.Now,
            "DecryptBackupCompleted",
            new Dictionary<string, object>
            {
                { "backupPath", backupPath },
                { "restorePath", restorePath },
                { "filesProcessed", filesProcessed },
                { "totalBytesTransferred", totalBytesTransferred }
            }
        );
    }

    //================================================//
    //          PRIORITY MANAGEMENT METHODS           //
    //================================================//

    private void RegisterJobForPriorityScan()
    {
        lock (_priorityLock)
        {
            _jobsWaitingToScan++;
            _priorityWaitHandle.Reset();
        }
    }

    private void ReportPriorityFilesFound(int count)
    {
        lock (_priorityLock)
        {
            _jobsWaitingToScan--;
            _priorityFilesPending += count;

            if (_jobsWaitingToScan == 0 && _priorityFilesPending == 0)
            {
                _priorityWaitHandle.Set();
            }
        }
    }

    private void ReportPriorityFileProcessed()
    {
        lock (_priorityLock)
        {
            _priorityFilesPending--;

            if (_jobsWaitingToScan == 0 && _priorityFilesPending == 0)
            {
                _priorityWaitHandle.Set();
            }
        }
    }
}
