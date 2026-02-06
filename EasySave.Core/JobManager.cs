namespace EasySave.Core;

using EasySave.Models;
using EasyLog.Lib;

public class JobManager
{
    private readonly List<Job> _jobs;
    private readonly EasyLog _logger;

    public JobManager()
    {
        _configParser = new ConfigParser("../config.json");
        _logFormatter = new JsonLogFormatter();
        _logger = new EasyLog(_logFormatter, _configParser.Config?["config"]?["logsPath"]?.GetValue<string>() ?? "logs.json");

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

        /// TODO : initialiser le stateTracker et logger son lancement

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
    }

    public void CreateJob(string name, JobType type, string sourcePath, string destinationPath)
    {
        var job = new Job(name, type, sourcePath, destinationPath);

        _jobs.Add(job);
    }

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

    public void removeJob(string name)
    {
        var jobToRemove = _jobs.FirstOrDefault(j => j.Name == name);

        if (jobToRemove == null)
        {
            return;
        }

        _jobs.Remove(jobToRemove);

        RemoveJobFromConfig(name);

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
        _jobs.Remove(job);
    }

    private void RemoveJobFromConfig(string jobName)
    {
        var jobsArray = _configParser.Config?["jobs"]?.AsArray();

        if (jobsArray == null)
        {
            return;
        }

        JsonNode? jobToRemove = null;
        foreach (var jobNode in jobsArray)
        {
            if (jobNode?["name"]?.GetValue<string>() == jobName)
            {
                jobToRemove = jobNode;
                break;
            }
        }

        if (jobToRemove != null)
        {
            jobsArray.Remove(jobToRemove);

            _configParser.EditAndSaveConfig(_configParser.Config!);
        }
    }

    public void LaunchJob(Job job)
    {
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
                    ExecuteFullBackup(job);
                    break;
                case JobType.Differential:
                    ExecuteDifferentialBackup(job);
                    break;
                default:
                    throw new InvalidOperationException($"Type de job non supporté : {job.Type}");
            }

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

    private void ExecuteFullBackup(Job job)
    {
        if (!Directory.Exists(job.SourcePath))
        {
            throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {job.SourcePath}");
        }

        if (!Directory.Exists(job.DestinationPath))
        {
            Directory.CreateDirectory(job.DestinationPath);
            _logger.Write(
                DateTime.Now,
                "DestinationDirectoryCreated",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "destinationPath", job.DestinationPath }
                }
            );
        }

        var sourceFiles = Directory.GetFiles(job.SourcePath, "*", SearchOption.AllDirectories);
        int totalFiles = sourceFiles.Length;
        int filesProcessed = 0;
        long totalBytesTransferred = 0;

        _logger.Write(
            DateTime.Now,
            "FullBackupStarted",
            new Dictionary<string, object>
            {
                { "jobName", job.Name },
                { "totalFiles", totalFiles }
            }
        );

        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);
                var destinationFile = Path.Combine(job.DestinationPath, relativePath);

                var destinationDir = Path.GetDirectoryName(destinationFile);
                if (destinationDir != null && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                var fileInfo = new FileInfo(sourceFile);
                var fileSize = fileInfo.Length;

                File.Copy(sourceFile, destinationFile, overwrite: true);

                filesProcessed++;
                totalBytesTransferred += fileSize;

                _logger.Write(
                    DateTime.Now,
                    "FileCopied",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "sourceFile", sourceFile },
                        { "destinationFile", destinationFile },
                        { "fileSize", fileSize },
                        { "progress", $"{filesProcessed}/{totalFiles}" }
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Write(
                    DateTime.Now,
                    "FileCopyError",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "sourceFile", sourceFile },
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
                { "totalBytesTransferred", totalBytesTransferred }
            }
        );
    }

    private void ExecuteDifferentialBackup(Job job)
    {
        // TODO : Implémenter la logique de sauvegarde différentielle
        throw new NotImplementedException("La sauvegarde différentielle n'est pas encore implémentée");
    }
}
