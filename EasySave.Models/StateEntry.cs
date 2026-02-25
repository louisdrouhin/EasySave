namespace EasySave.Models;

// Represents real-time state of a backup job
// Contains basic info (name, state, timestamp) and details if Active
public class StateEntry
{
    public string JobName { get; set; }
    public DateTime LastActionTime { get; set; }
    public JobState State { get; set; }

    // Fields populated only when State == Active
    public int? TotalFiles { get; set; }
    public long? TotalSizeToTransfer { get; set; }
    public double? Progress { get; set; }
    public int? RemainingFiles { get; set; }
    public long? RemainingSizeToTransfer { get; set; }
    public string? CurrentSourcePath { get; set; }
    public string? CurrentDestinationPath { get; set; }

    // Empty constructor for JSON serialization
    public StateEntry()
    {
        JobName = string.Empty;
    }

    // Creates StateEntry with basic info (for Inactive state)
    // @param jobName - job name
    // @param lastActionTime - date/time of last action
    // @param state - current job state
    public StateEntry(string jobName, DateTime lastActionTime, JobState state)
    {
        JobName = jobName;
        LastActionTime = lastActionTime;
        State = state;
    }

    // Creates complete StateEntry with progress (for Active state)
    // @param jobName - job name
    // @param lastActionTime - date/time of last action
    // @param state - job state
    // @param totalFiles - total number of files to process
    // @param totalSizeToTransfer - total size in bytes
    // @param progress - progress percentage (0-100)
    // @param remainingFiles - remaining files to process
    // @param remainingSizeToTransfer - remaining size in bytes
    // @param currentSourcePath - path of file being processed
    // @param currentDestinationPath - destination path of current file
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

    // Returns string representation of job state
    // Includes progress if job is executing
    // @returns formatted string with all relevant information
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
