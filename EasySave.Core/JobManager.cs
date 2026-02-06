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
        /// TODO : logger le lancement du configparser 
        /// TODO : logger le lancement du logger et fromatter 
        /// TODO : initialiser le stateTracker et logger son lancement

        _jobs = new List<Job>();

        var logFormatter = new JsonLogFormatter();
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "easysave.log");
        _logger = new EasyLog(logFormatter, logPath);
    }

    public void AddJob(Job job)
    {
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
}
