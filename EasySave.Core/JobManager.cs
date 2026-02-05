namespace EasySave.Core;

using EasySave.Models;
using EasyLog.Lib;

public class JobManager
{
    private readonly List<Job> _jobs;
    private readonly EasyLog _logger;

    public JobManager()
    {
        _jobs = new List<Job>();

        var logFormatter = new JsonLogFormatter();
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "easysave.log");
        _logger = new EasyLog(logFormatter, logPath);
    }

    public void AddJob(Job job)
    {
        _jobs.Add(job);
    }

    public void RemoveJob(Job job)
    {
        _jobs.Remove(job);
    }


}
