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

    private void ExecuteFullBackup(Job job, bool createHashFile = false)
    {
        if (!Directory.Exists(job.SourcePath))
        {
            throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {job.SourcePath}");
        }

        // Créer le répertoire de destination principal s'il n'existe pas
        if (!Directory.Exists(job.DestinationPath))
        {
            Directory.CreateDirectory(job.DestinationPath);
        }

        // Créer un sous-dossier avec timestamp pour cette sauvegarde
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
        int filesProcessed = 0;
        long totalBytesTransferred = 0;
        var hashDictionary = createHashFile ? new Dictionary<string, string>() : null;

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

                File.Copy(sourceFile, destinationFile, overwrite: true);

                // Calculer le hash SHA-256 seulement si demandé
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
                                { "sourceFile", sourceFile },
                                { "error", hashEx.Message }
                            }
                        );
                    }
                }

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

        // Créer/Mettre à jour le fichier hash.json seulement si demandé
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

    private void ExecuteDifferentialBackup(Job job)
    {
        // Vérifier que le répertoire source existe
        if (!Directory.Exists(job.SourcePath))
        {
            throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {job.SourcePath}");
        }

        // Créer le répertoire de destination principal s'il n'existe pas
        if (!Directory.Exists(job.DestinationPath))
        {
            Directory.CreateDirectory(job.DestinationPath);
        }

        // Vérifier si le fichier hash.json existe
        var hashFilePath = Path.Combine(job.DestinationPath, "hash.json");
        if (!File.Exists(hashFilePath))
        {
            _logger.Write(
                DateTime.Now,
                "NoHashFileFound",
                new Dictionary<string, object>
                {
                    { "jobName", job.Name },
                    { "message", "Aucun fichier hash.json trouvé, basculement vers une sauvegarde complète" }
                }
            );

            // Bascule automatiquement vers une sauvegarde complète avec création du fichier hash
            ExecuteFullBackup(job, createHashFile: true);
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
            throw new InvalidOperationException($"Erreur lors de la lecture du fichier hash.json : {ex.Message}", ex);
        }

        // Créer le dossier de sauvegarde différentielle avec timestamp
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

        int totalFiles = 0;
        int filesProcessed = 0;
        int filesModified = 0;
        long totalBytesTransferred = 0;
        var newHashDictionary = new Dictionary<string, string>();

        // Parcourir séquentiellement tous les fichiers de la source
        foreach (var sourceFile in Directory.EnumerateFiles(job.SourcePath, "*", SearchOption.AllDirectories))
        {
            totalFiles++;
            string currentHash = string.Empty;

            try
            {
                // Calculer le hash SHA-256 du fichier
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                using (var stream = File.OpenRead(sourceFile))
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    currentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                // Obtenir le chemin relatif
                var relativePath = Path.GetRelativePath(job.SourcePath, sourceFile);

                // Stocker le nouveau hash
                newHashDictionary[relativePath] = currentHash;

                // Vérifier si le fichier a été modifié ou est nouveau
                bool needsCopy = false;
                if (!hashDictionary.ContainsKey(relativePath))
                {
                    needsCopy = true;
                    _logger.Write(
                        DateTime.Now,
                        "NewFileDetected",
                        new Dictionary<string, object>
                        {
                            { "jobName", job.Name },
                            { "file", relativePath }
                        }
                    );
                }
                else if (hashDictionary[relativePath] != currentHash)
                {
                    needsCopy = true;
                    _logger.Write(
                        DateTime.Now,
                        "ModifiedFileDetected",
                        new Dictionary<string, object>
                        {
                            { "jobName", job.Name },
                            { "file", relativePath },
                            { "oldHash", hashDictionary[relativePath] },
                            { "newHash", currentHash }
                        }
                    );
                }

                // Copier le fichier s'il a été modifié ou est nouveau
                if (needsCopy)
                {
                    var destinationFile = Path.Combine(diffBackupPath, relativePath);
                    var destinationDir = Path.GetDirectoryName(destinationFile);

                    if (destinationDir != null && !Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    var fileInfo = new FileInfo(sourceFile);
                    var fileSize = fileInfo.Length;

                    File.Copy(sourceFile, destinationFile, true);

                    filesModified++;
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
                            { "hash", currentHash }
                        }
                    );
                }

                filesProcessed++;
            }
            catch (Exception ex)
            {
                _logger.Write(
                    DateTime.Now,
                    "FileProcessError",
                    new Dictionary<string, object>
                    {
                        { "jobName", job.Name },
                        { "sourceFile", sourceFile },
                        { "error", ex.Message }
                    }
                );
                // Continuer avec les autres fichiers
            }
        }

        // Mettre à jour le fichier hash.json avec les nouveaux hash
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
                { "totalFiles", totalFiles },
                { "filesProcessed", filesProcessed },
                { "filesModified", filesModified },
                { "totalBytesTransferred", totalBytesTransferred },
                { "backupFolder", backupFolderName }
            }
        );
    }
}
