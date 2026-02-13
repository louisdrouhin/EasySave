namespace EasySave.Core;

using EasySave.Models;
using EasyLog.Lib;
using EasySave.Core.Localization;
using System.Text.Json.Nodes;
using System.Diagnostics;

/// <summary>
/// Event arguments for log format change events
/// </summary>
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

    public ConfigParser ConfigParser => _configParser;

    /// <summary>
    /// Event triggered when the log format changes
    /// </summary>
    public event EventHandler<LogFormatChangedEventArgs>? LogFormatChanged;

    public JobManager()
    {
        _configParser = new ConfigParser("config.json");
        _logFormatter = CreateLogFormatter();
        _logger = new EasyLog(_logFormatter, _configParser.GetLogsPath());

        _logger.Write(
            DateTime.Now,
            "ConfigParserInitialized",
            new Dictionary<string, object>
            {
                { "configPath", "../config.json" }
            }
        );

        _logger.Write(
            DateTime.Now,
            "LoggerInitialized",
            new Dictionary<string, object>
            {
                { "formatterType", _logFormatter.GetType().Name },
                { "logsPath", _configParser.Config?["config"]?["logsPath"]?.GetValue<string>() ?? "logs.json" }
            }
        );

        _jobs = new List<Job>();

        _logger.Write(
            DateTime.Now,
            "JobsListCreated",
            new Dictionary<string, object>()
        );

        LoadJobsFromConfig();

        _logger.Write(
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

        _logger.Write(
            DateTime.Now,
            "StateTrackerCreated",
            new Dictionary<string, object>()
        );
    }

    private ILogFormatter CreateLogFormatter()
    {
        string logFormat = _configParser.Config?["config"]?["logFormat"]?.GetValue<string>()?.ToLower() ?? "json";

        return logFormat switch
        {
            "xml" => new XmlLogFormatter(),
            "json" => new JsonLogFormatter(),
            _ => new JsonLogFormatter() // Format par défaut
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

        _logger.Write(
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

        _logger.Write(
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
        _logger.Close();
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
                    _logger.Write(
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
                _logger.Write(
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

        _configParser.SetLogFormat(format);

        _configParser.LoadConfig();

        _logFormatter = CreateLogFormatter();

        _logger = new EasyLog(_logFormatter, _configParser.GetLogsPath());

        _logger.Write(
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

    public void LaunchJob(Job job, string password)
    {
        _stateTracker.UpdateJobState(
            new StateEntry(
                job.Name,
                DateTime.Now,
                JobState.Active
              )
            );

        _logger.Write(
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
                    ExecuteFullBackup(job, password);
                    break;
                case JobType.Differential:
                    ExecuteDifferentialBackup(job, password);
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

            _logger.Write(
                DateTime.Now,
                "JobCompleted",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "jobType", job.Type.ToString() }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.Write(
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
    }

    private void ExecuteFullBackup(Job job, string password, bool createHashFile = false)
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

        _logger.Write(
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

        _logger.Write(
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

        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
                var destinationFile = Path.Combine(fullBackupPath, relativePath);

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
                        _logger.Write(
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

                    _logger.Write(
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

                    _logger.Write(
                        DateTime.Now,
                        "FileEncryptionError",
                        logData
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Write(
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

                _logger.Write(
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
                _logger.Write(
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

        _logger.Write(
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

    private void ExecuteDifferentialBackup(Job job, string password)
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
            _logger.Write(
                DateTime.Now,
                "NoHashFileFound",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "message", "No hash.json file found, switching to full backup" }
                }
            );

            ExecuteFullBackup(job, password, createHashFile: true);
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

        _logger.Write(
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
                _logger.Write(
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

        foreach (var fileToCopy in filesToCopy)
        {
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

                    _logger.Write(
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

                    _logger.Write(
                        DateTime.Now,
                        "FileEncryptionError",
                        logData
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Write(
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

        try
        {
            var newHashJson = System.Text.Json.JsonSerializer.Serialize(newHashDictionary, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(hashFilePath, newHashJson);

            _logger.Write(
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
            _logger.Write(
                DateTime.Now,
                "HashFileUpdateError",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "error", ex.Message }
                }
            );
        }

        _logger.Write(
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

        if (encryptExtensions.Contains(fileExtension))
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
            //TODO: manage password for no-null and no space

            // Remonte depuis bin/Debug/net10.0 jusqu'à la racine du projet
            var appDirectory = AppContext.BaseDirectory;
            var projectRootDirectory = Path.Combine(appDirectory, "..", "..", "..", "..");
            var cryptosoftPath = Path.Combine(projectRootDirectory, "Cryptosoft.exe");

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
                    _logger.Write(
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
            _logger.Write(
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

        _logger.Write(
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

                _logger.Write(
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
                _logger.Write(
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

        _logger.Write(
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

}