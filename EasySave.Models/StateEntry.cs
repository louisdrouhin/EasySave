namespace EasySave.Models;

public class StateEntry
{
    public string JobName { get; set; }
    public DateTime LastActionTime { get; set; }
    public JobState State { get; set; }

    // Fields if state == Active
    public int? TotalFiles { get; set; }
    public long? TotalSizeToTransfer { get; set; }
    public double? Progress { get; set; }
    public int? RemainingFiles { get; set; }
    public long? RemainingSizeToTransfer { get; set; }
    public string? CurrentSourcePath { get; set; }
    public string? CurrentDestinationPath { get; set; }

    public StateEntry()
    {
        JobName = string.Empty;
    }

    public StateEntry(string jobName, DateTime lastActionTime, JobState state)
    {
        JobName = jobName;
        LastActionTime = lastActionTime;
        State = state;
    }

    public StateEntry(
        string jobName,
        DateTime lastActionTime,
        JobState state,
        int totalFiles = 0,
        long totalSizeToTransfer = 0,
        double progress = 0,
        int remainingFiles = 0,
        long remainingSizeToTransfer = 0,
        string currentSourcePath = "",
        string currentDestinationPath = "")
    {
        JobName = jobName;
        LastActionTime = lastActionTime;
        State = state;
        TotalFiles = totalFiles;
        TotalSizeToTransfer = totalSizeToTransfer;
        Progress = progress;
        RemainingFiles = remainingFiles;
        RemainingSizeToTransfer = remainingSizeToTransfer;
        CurrentSourcePath = currentSourcePath;
        CurrentDestinationPath = currentDestinationPath;
    }

    public override string ToString()
    {
        var baseInfo = $"JobName={JobName}, LastActionTime={LastActionTime:yyyy-MM-dd HH:mm:ss.fff}, State={State}";

        if (State == JobState.Inactive)
        {
            return baseInfo;
        }

        return $"{baseInfo}, TotalFiles={TotalFiles}, TotalSizeToTransfer={TotalSizeToTransfer}, Progress={Progress}%, RemainingFiles={RemainingFiles}, RemainingSizeToTransfer={RemainingSizeToTransfer}, CurrentSourcePath={CurrentSourcePath}, CurrentDestinationPath={CurrentDestinationPath}";
    }
}
